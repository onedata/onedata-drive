using NLog;
using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive
{
    public class PlaceholderFetcher
    {
        private Logger logger;
        private LoggerFormater loggerFormater;
        public PlaceholderFetcher(Logger logger)
        {
            this.logger = logger;
            this.loggerFormater = new(logger);
        }

        public void FetchPlaceholders(Callback callback)
        {
            string opID = IdGenerator.GenerateId8();
            Func<CancellationToken, Task> func = (token) => FetchPlaceholdersAsync(callback, token, opID);

            CloudSync.runningTasks.AddTask(
                func,
                TaskType.FETCH_DATA,
                opID);
        }

        private async Task FetchPlaceholdersAsync(Callback callback, CancellationToken token, string opID)
        {
            List<string> moreInfo = new() {
                $"Path:     {callback.filePath}"
            };
            loggerFormater.LogFileOP(LogLevel.Info, "FETCH PLACEHOLDERS", "START", moreInfo, callback.filePath, opID: opID);

            CF_OPERATION_INFO oi = new()
            {
                Type = CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_PLACEHOLDERS,
                ConnectionKey = callback.connectionKey,
                TransferKey = callback.transferKey
            };
            oi.StructSize = (uint)Marshal.SizeOf(oi);

            //PlaceholderCreateInfo placeholderCreateInfo = new();
            CF_PLACEHOLDER_CREATE_INFO[] infoArr = [];

            try
            {
                CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS tp;
                string folderPath = PathUtils.GetFullPath(callback);
                bool addToMonitored = false;

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


                    /*
                    placeholderCreateInfo = Placeholders.FetchPlaceholdersInfo(folderPath);

                    int placeholderArrLen = 0;
                    if (placeholderCreateInfo.Count() > 0)
                    {
                        CF_PLACEHOLDER_CREATE_INFO[] placeholderArr = placeholderCreateInfo.GetArray();
                        placeholderArrLen = placeholderCreateInfo.Get().Count;

                        // copy arr to unmanaged memory
                        placeholderArrayMemory.Allocate((uint)(Marshal.SizeOf(typeof(CF_PLACEHOLDER_CREATE_INFO)) * placeholderArrLen));
                        for (int i = 0; i < placeholderArrLen; i++)
                        {
                            Marshal.StructureToPtr(placeholderArr[i], placeholderArrayMemory.GetPointer() + (i * Marshal.SizeOf(typeof(CF_PLACEHOLDER_CREATE_INFO))), false);
                        }
                    }

                    
                    CF_OPERATION_PARAMETERS op = RegularPlaceholdersParams(placeholderArrLen, placeholderArrayMemory, flags);

                    CfExecuteWrapper(oi, ref op);
                    */




                    using UnmanagedMem placeholderArrayMemory = new UnmanagedMem();
                    string parentId = PathUtils.GetPlaceholderId(folderPath);
                    SpaceFolder space = PathUtils.GetSpaceFolder(folderPath);


                    string nextPageToken = "";
                    do
                    {
                        CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_STOP_ON_ERROR;

                        DirChildren dirChildren = await RestClient.GetFilesAndSubdirs(parentId, space.providerInfos, nextPageToken, token);
                        nextPageToken = dirChildren.nextPageToken;

                        using PlaceholderCreateInfo placeholderCreateInfo = new();
                        foreach (Child child in dirChildren.children)
                        {
                            string windowsCorrectName = NameConvertor.DistinctWindowsName(child, placeholderCreateInfo);
                            PlaceholderData data = new(child.file_id, windowsCorrectName, child.size, child.atime, child.mtime, child.ctime, child.type);
                            placeholderCreateInfo.Add(Placeholders.CreateInfo(data));
                        }

                        // fetch placeholders
                        // create placeholder array in unmanaged memory
                        // if there is no next page token, set flags ...

                        if (string.IsNullOrEmpty(nextPageToken))
                        {
                            flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_DISABLE_ON_DEMAND_POPULATION
                                | CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_STOP_ON_ERROR;
                        }

                        CfExecuteWrapper(oi, ref op);
                    }
                    while (!string.IsNullOrEmpty(nextPageToken));


                    addToMonitored = true;
                }
                
                loggerFormater.LogFileOP(LogLevel.Info, "FETCH PLACEHOLDERS", "OK", opID: opID);

                if (addToMonitored)
                {
                    string spaceName = PathUtils.GetSpaceName(folderPath);
                    if (CloudSync.spaces.TryGetValue(spaceName, out SpaceFolder? spaceFolder))
                    {
                        spaceFolder.autoRefresh?.AddToMonitored(PathUtils.GetPlaceholderId(folderPath), folderPath);
                        Debug.Print("Added to monitored: {0} - {1}", spaceName, folderPath);
                    }
                    else
                    {
                        Debug.Print("Failed to find SPACE");
                    }
                }
                return;
            }
            catch (Exception e)
            {
                logger.Error(e, $"Error in FetchPlaceholdersAsync for path {callback.filePath}");
                NTStatus status = new NTStatus((uint)CloudFilterEnum.STATUS_CLOUD_FILE_UNSUCCESSFUL);
                if (e.InnerException is NoSuchCloudFile)
                {
                    status = new NTStatus((uint)CloudFilterEnum.STATUS_NOT_A_CLOUD_FILE);
                }
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
            finally
            {
                placeholderCreateInfo.Dispose();
                loggerFormater.LogFileOP(LogLevel.Info, "FETCH PLACEHOLDERS", "FINISHED", opID: opID);
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

        private CF_OPERATION_PARAMETERS RegularPlaceholdersParams(int placeholderArrLen, 
            UnmanagedMem placeholderArrayMemory, CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS flags)
        {
            CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS tp = new()
            {
                CompletionStatus = NTStatus.STATUS_SUCCESS,
                Flags = flags,
                PlaceholderTotalCount = placeholderArrLen,
                EntriesProcessed = 0,
                PlaceholderCount = (uint)placeholderArrLen,
                PlaceholderArray = placeholderArrayMemory.GetPointer()
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
    }
}
