using NLog;
using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive
{
    public class PlaceholderFetcher
    {
        private LoggerFormater loggerFormater;
        public PlaceholderFetcher(Logger logger)
        {
            this.loggerFormater = new(logger);
        }

        public void FetchPlaceholders(Callback callback)
        {
            string opID = IdGenerator.GenerateId8();
            Func<CancellationToken, Task> fetchFunc = (token) => FetchPlaceholdersAsync(callback, token, opID);

            if (CloudSync.runningTasks.list.Any(x => x.type == TaskType.FETCH_PLACEHOLDERS && x.callback?.filePath == callback.filePath && !x.task.IsCompleted))
            {
                loggerFormater.LogFileOP(LogLevel.Info, "FETCH PLACEHOLDERS", "Already running for this path, skipping", filePath: callback.filePath, opID: opID);
                return;
            }

            CloudSync.runningTasks.AddTask(
                fetchFunc,
                TaskType.FETCH_PLACEHOLDERS,
                callback,
                opID);
        }

        private async Task FetchPlaceholdersAsync(Callback callback, CancellationToken token, string opID)
        {
            loggerFormater.LogFileOP(LogLevel.Info, "FETCH PLACEHOLDERS", "START", filePath: callback.filePath, opID: opID);

            const uint PLACEHOLDER_BATCH_SIZE = 1000;
            CF_OPERATION_INFO oi = new()
            {
                Type = CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_PLACEHOLDERS,
                ConnectionKey = callback.connectionKey,
                TransferKey = callback.transferKey
            };
            oi.StructSize = (uint)Marshal.SizeOf(oi);

            CF_PLACEHOLDER_CREATE_INFO[] infoArr = [];

            try
            {
                string folderPath = PathUtils.GetFullPath(callback);

                if (!Directory.Exists(folderPath))
                {
                    throw new Exception($"Directory does not exist: {folderPath}");
                }
                else if (PathUtils.IsRootPath(folderPath))
                {
                    loggerFormater.LogFileOP(LogLevel.Info, "FETCH PLACEHOLDERS", "spaces", opID: opID);

                    CF_OPERATION_PARAMETERS op = SpacePlaceholdersParams();
                    CfExecuteWrapper(oi, ref op);
                }
                else
                {
                    loggerFormater.LogFileOP(LogLevel.Info, "FETCH PLACEHOLDERS", "standard directory", opID: opID);

                    HashSet<string> placeholderNames = new HashSet<string>();
                    using UnmanagedMem placeholderArrayMemory = new UnmanagedMem((uint)(Marshal.SizeOf(typeof(CF_PLACEHOLDER_CREATE_INFO)) * PLACEHOLDER_BATCH_SIZE));
                    string parentId = PathUtils.GetPlaceholderId(folderPath);
                    SpaceFolder space = PathUtils.GetSpaceFolder(folderPath);
                    int placeholderTotalCount = 0;
                    int entriesProcessed = 0;

                    string nextPageToken = "";
                    bool isLast = true;
                    do
                    {
                        CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_DISABLE_ON_DEMAND_POPULATION;

                        // fetch placeholders - done
                        DirChildren dirChildren = await RestClient.GetFilesAndSubdirs(parentId, space.providerInfos, limit: PLACEHOLDER_BATCH_SIZE, nextPageToken: nextPageToken, cancelToken: token);
                        nextPageToken = dirChildren.nextPageToken;
                        isLast = dirChildren.isLast;

                        // create placeholder create infos and make names distinct
                        using PlaceholderCreateInfoList placeholderCreateInfo = new();
                        foreach (Child child in dirChildren.children)
                        {
                            string windowsCorrectName = NameConvertor.DistinctWindowsName(child, placeholderNames);
                            PlaceholderData data = new(child.fileId, windowsCorrectName, child.size, child.atime, child.mtime, child.ctime, child.type);
                            placeholderCreateInfo.Add(PlaceholderData.CreateInfo(data));
                            placeholderNames.Add(windowsCorrectName.ToLower());
                        }
                        placeholderTotalCount += placeholderCreateInfo.Count();

                        // create placeholder array in unmanaged memory
                        if (placeholderCreateInfo.Count() > PLACEHOLDER_BATCH_SIZE)
                        {
                            throw new Exception($"PlaceholderCreateInfoList contains more items ({placeholderCreateInfo.Count()}) than the defined batch size ({PLACEHOLDER_BATCH_SIZE}).");
                        }
                        for (int i = 0; i < placeholderCreateInfo.Count(); i++)
                        {
                            Marshal.StructureToPtr(placeholderCreateInfo[i], placeholderArrayMemory.GetPointer() + (i * Marshal.SizeOf(typeof(CF_PLACEHOLDER_CREATE_INFO))), false);
                        }

                        // if there is no next page cancelToken, set flags ...
                        if (dirChildren.isLast)
                        {
                            flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_DISABLE_ON_DEMAND_POPULATION
                                | CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_STOP_ON_ERROR;
                        }

                        CF_OPERATION_PARAMETERS op = RegularPlaceholdersParams(entriesProcessed, placeholderCreateInfo.Count(), placeholderArrayMemory, flags);
                        CfExecuteWrapper(oi, ref op);
                        entriesProcessed += placeholderCreateInfo.Count();
                        
                        token.ThrowIfCancellationRequested();
                    }
                    while (!isLast);

                    string spaceName = PathUtils.GetSpaceName(folderPath);
                    if (CloudSync.spaces.TryGetValue(spaceName, out SpaceFolder? spaceFolder))
                    {
                        spaceFolder.autoRefresh?.AddToMonitored(PathUtils.GetPlaceholderId(folderPath), folderPath);
                        loggerFormater.LogFileOP(LogLevel.Info, "FETCH PLACEHOLDERS", "Directory added to monitored", opID: opID);
                    }
                    else
                    {
                        loggerFormater.LogFileOP(LogLevel.Info, "FETCH PLACEHOLDERS", "FAILED to add directory to monitored", opID: opID);
                    }
                }

                loggerFormater.LogFileOP(LogLevel.Info, "FETCH PLACEHOLDERS", "OK", opID: opID);
                return;
            }
            catch (TaskCanceledException e)
            {
                loggerFormater.LogFileOP(LogLevel.Info, "FETCH PLACEHOLDERS", "Canceled", e, opID: opID);
            }
            catch (NoSuchCloudFile e)
            {
                loggerFormater.LogFileOP(LogLevel.Error, "FETCH PLACEHOLDERS", "NoSuchCloudFile", e, opID: opID);
                NTStatus status = new NTStatus((uint)CloudFilterEnum.STATUS_NOT_A_CLOUD_FILE);
                LastResortFailureHandler(oi, status, opID);
                try
                {
                    DirectoryOD.Delete(callback);
                    loggerFormater.LogFileOP(LogLevel.Info, "FETCH PLACEHOLDERS",
                        "Deleted placeholder directory after NoSuchCloudFile exception", opID: opID);
                }
                catch (Exception ex)
                {
                    loggerFormater.LogFileOP(LogLevel.Error, "FETCH PLACEHOLDERS",
                        "Failed to delete placeholder directory after NoSuchCloudFile exception", ex, opID: opID);
                }
            }
            catch (Exception e)
            {
                loggerFormater.LogFileOP(LogLevel.Error, "FETCH PLACEHOLDERS", "FAIL", e, opID: opID);
                NTStatus status = new NTStatus((uint)CloudFilterEnum.STATUS_CLOUD_FILE_UNSUCCESSFUL);
                LastResortFailureHandler(oi, status, opID);
            }
        }


        private void LastResortFailureHandler(CF_OPERATION_INFO oi, NTStatus status, string opID)
        {
            CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS tp = new()
            {
                Flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_STOP_ON_ERROR,
                CompletionStatus = status,
                PlaceholderTotalCount = 0,
                EntriesProcessed = 0,
                PlaceholderArray = IntPtr.Zero,
                PlaceholderCount = 0
            };
            CF_OPERATION_PARAMETERS op = CF_OPERATION_PARAMETERS.Create(tp);

            try
            {
                CfExecuteWrapper(oi, ref op);
            }
            catch (Exception ex)
            {
                loggerFormater.LogFileOP(LogLevel.Error, "FETCH PLACEHOLDERS", "FAIL - last resort CfExecute", ex, opID: opID);
            }
        }

        private CF_OPERATION_PARAMETERS SpacePlaceholdersParams()
        {
            CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS tp = new()
            {
                Flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_DISABLE_ON_DEMAND_POPULATION
                            | CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_STOP_ON_ERROR,
                CompletionStatus = NTStatus.STATUS_SUCCESS,
                PlaceholderTotalCount = 0,
                EntriesProcessed = 0,
                PlaceholderCount = 0
            };
            return CF_OPERATION_PARAMETERS.Create(tp);
        }

        private CF_OPERATION_PARAMETERS RegularPlaceholdersParams(int entriesProcessed, int placeholderArrLen,
            UnmanagedMem placeholderArrayMemory, CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS flags)
        {
            nint placeholderArrayPtr = IntPtr.Zero;
            if (placeholderArrLen > 0)
            {
                placeholderArrayPtr = placeholderArrayMemory.GetPointer();
            }
            CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS tp = new()
            {
                CompletionStatus = NTStatus.STATUS_SUCCESS,
                Flags = flags,
                PlaceholderTotalCount = placeholderArrLen + entriesProcessed,
                EntriesProcessed = (uint)entriesProcessed,
                PlaceholderCount = (uint)placeholderArrLen,
                PlaceholderArray = placeholderArrayPtr
            };
            return CF_OPERATION_PARAMETERS.Create(tp);
        }

        private void CfExecuteWrapper(CF_OPERATION_INFO oi, ref CF_OPERATION_PARAMETERS op)
        {
            if (op.ParamSize == 0)
            {
                throw new ArgumentException("CF_OPERATION_PARAMETERS is not properly initialized. ParamSize is 0.");
            }

            HRESULT hres = CfExecute(oi, ref op);
            if (hres != NTStatus.STATUS_SUCCESS)
            {
                throw new Exception($"Fetch placeholders CfExecute FAIL - HRES 0x{((uint)hres):X}: {hres}");
            }
        }

        public void CancelFetchPlaceholders(Callback callback)
        {
            string opID = IdGenerator.GenerateId8();
            loggerFormater.LogFileOP(LogLevel.Info, "CANCEL FETCH PLACEHOLDERS", "START", filePath: callback.filePath, opID: opID);
            List<RunningTask> toCancel = CloudSync.runningTasks.list.FindAll(
                x => x.type == TaskType.FETCH_PLACEHOLDERS
                && x.callback?.filePath == callback.filePath
                && !x.task.IsCompleted);
            toCancel.ForEach(x => x.Cancel());
            List<string> canceledIDs = toCancel.Select(x => x.opID).ToList();
            List<string> moreInfo = new List<string>
            {
                "Canceled IDs:"
            };
            moreInfo.AddRange(canceledIDs);
            loggerFormater.LogFileOP(LogLevel.Info, "CANCEL FETCH PLACEHOLDERS", $"Canceled count: {toCancel.Count}", opID: opID, moreInfo: canceledIDs);
        }
    }
}
