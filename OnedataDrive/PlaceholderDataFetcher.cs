using NLog;
using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive
{
    public class PlaceholderDataFetcher : IDisposable
    {
        private ThreadSafeList<RunningTask> runningTasks;
        private CancellationTokenSource masterTokenSource;
        private Logger logger;
        private LoggerFormater loggerFormater;
        public PlaceholderDataFetcher(Logger logger)
        {
            this.runningTasks = new();
            this.masterTokenSource = new();
            this.logger = logger;
            this.loggerFormater = new(logger);
        }
        public void FetchData(FetchDataCallback callback)
        {
            string opID = IdGenerator.GenerateId8();
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(masterTokenSource.Token);
            RunningTask runningTask = new(FetchDataType.FETCH_DATA, callback, Task.CompletedTask, cts, opID);
            runningTasks.Add(runningTask);
            runningTask.task = FetchDataAsync(callback, cts.Token, runningTask, opID);
        }
        private async Task FetchDataAsync(FetchDataCallback callback, CancellationToken token, RunningTask thisTask, string opID)
        {
            List<string> moreInfo = new() {
                $"Path: {callback.filePath}", 
                $"Offset: {callback.offset}",
                $"OffsetLength: {callback.length}"};
            loggerFormater.LogFileOP(LogLevel.Info, "FETCH DATA", "START", moreInfo, callback.filePath, opID: opID);

            CF_OPERATION_INFO oi = new()
            {
                Type = CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_DATA,
                ConnectionKey = callback.connectionKey,
                TransferKey = callback.transferKey
            };
            oi.StructSize = (uint)Marshal.SizeOf(oi);

            IntPtr unmanagedPointer = IntPtr.Zero;
            CF_OPERATION_PARAMETERS.TRANSFERDATA td;
            CF_OPERATION_PARAMETERS op;

            try
            {
                string fileIdentity = callback.fileIdentity;
                SpaceFolder space = CloudSync.spaces[PathUtils.GetSpaceName(callback.filePath)];

                FileAttribute fileInfo = await RestClient.GetFileAttribute(fileIdentity, space.providerInfos, token);
                if (fileInfo.size != callback.fileSize)
                {
                    throw new PlaceholderSizeException("Size of cloud file does not match local file size.");
                    // TODO: update placeholder, so operation runs OK
                }
                using (LivelinessChcecker livelinessChcecker = new(10 * 1000, turnOffWhenDead: false))
                using (Stream stream = await RestClient.GetStream(
                    CloudSync.spaces[PathUtils.GetSpaceName(callback.filePath)].providerInfos,
                    callback.fileIdentity,
                    token,
                    callback.offset,
                    callback.offset + callback.length - 1))
                {
                    const int CHUNK = 4096 * 4;
                    unmanagedPointer = Marshal.AllocHGlobal(CHUNK);
                    byte[] buffer = new byte[CHUNK];
                    int read;
                    long offset = callback.offset;

                    td = new()
                    {
                        CompletionStatus = new NTStatus((uint)CloudFilterEnum.STATUS_SUCCESS),
                        Buffer = unmanagedPointer,
                        Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE
                    };

                    livelinessChcecker.Start();
                    do
                    {
                        read = 0;
                        do
                        {
                            read += await stream.ReadAsync(buffer, read, CHUNK - read, token);
                            if (read == 0)
                            {
                                Debug.Print("Fetch Data - End of stream");
                                throw new Exception("Fetch Data - End of stream");
                            }
                            livelinessChcecker.IamAlive();
                        } while (read < CHUNK && (read + offset) < (callback.offset + callback.length) && !token.IsCancellationRequested);

                        Marshal.Copy(buffer, 0, unmanagedPointer, read);

                        td.Length = read;
                        td.Offset = offset;

                        offset += read;

                        op = CF_OPERATION_PARAMETERS.Create(td);

                        CfReportProviderProgress(callback.connectionKey, callback.transferKey, callback.fileSize, offset);

                        HRESULT hres = CfExecute(oi, ref op);
                        if (hres != HRESULT.S_OK)
                        {
                            throw new Exception($"Fetch data CfExecute FAIL - HRES: {hres}");
                        }
                        callback.alreadyFetchedOffset = offset;
                    } while (offset < (callback.offset + callback.length) && !token.IsCancellationRequested);
                }
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
                loggerFormater.LogFileOP(LogLevel.Info, "FETCH DATA", "OK", opID: opID);
            }
            catch (AggregateException e) when (e.InnerException is NoSuchCloudFile)
            {
                td = new()
                {
                    CompletionStatus = new NTStatus((uint)CloudFilterEnum.STATUS_NOT_A_CLOUD_FILE),
                    Buffer = unmanagedPointer,
                    Offset = 0,
                    Length = 4096,
                    Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE
                };

                op = CF_OPERATION_PARAMETERS.Create(td);

                HRESULT hres = CfExecute(oi, ref op);

                Exception ex = e;
                if (hres != HRESULT.S_OK)
                {
                    ex = new Exception($"CfExecute Stop operation HRES: {hres}", e);
                }

                loggerFormater.LogFileOP(LogLevel.Error, "FETCH DATA", "FAIL - No such file", e, opID: opID);

                File.Delete(callback.filePath);
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException)
                {
                    Debug.Print("OperationCanceledException - opID: {0}", opID);
                }
                // TODO: CompletionStatus = new NTStatus((uint)CloudFilterEnum.STATUS_CLOUD_FILE_REQUEST_ABORTED) - seems to be wrong
                // It does not terminate fetch request (copy window does not close)
                // UPDATE: it seems that "Length" must contain n*4096, where n >= 1, otherwise CfExecute fails
                td = new()
                {
                    CompletionStatus = new NTStatus((uint)CloudFilterEnum.STATUS_CLOUD_FILE_UNSUCCESSFUL),
                    Buffer = 0,
                    Offset = 0,
                    Length = 4096,
                    Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE
                };

                op = CF_OPERATION_PARAMETERS.Create(td);

                HRESULT hres = CfExecute(oi, ref op);
                if (hres != HRESULT.S_OK)
                {
                    e = new Exception($"CfExecute Stop operation HRES: {hres}", e);
                }
                loggerFormater.LogFileOP(LogLevel.Error, "FETCH DATA", "FAIL", e, opID: opID);
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedPointer);
                bool removed = runningTasks.Remove(thisTask);
                Debug.Print("Task {0} removed from list: {1}", opID, removed);
            }
        }

        public void CancelFetchData(FetchDataCallback callback)
        {
            string opID = IdGenerator.GenerateId8();
            CancelFetchDataAsync(callback, opID);
        }

        private async void CancelFetchDataAsync(FetchDataCallback callback, string opID)
        {
            Debug.Print("Cancel fetch - START");
            await Task.Run(() => { 
                long cancelStart = callback.offset;
                long cancelEnd = callback.offset + callback.length;

                List<RunningTask> terminateList = runningTasks.FindAll(
                    x => x.callback.normalizedPath == callback.normalizedPath
                    && x.callback.alreadyFetchedOffset >= cancelStart
                    && (x.callback.offset + x.callback.length) <= cancelEnd);

                foreach (RunningTask task in terminateList)
                {
                    Debug.Print("Task canceled - opID: {0}", task.opID);
                    task.Cancel();
                }
            });
            Debug.Print("Cancel fetch - END");
        }

        public void Dispose()
        {
            masterTokenSource.Cancel();
        }
    }

    public class FetchDataCallback : Callback
    {
        public long offset;
        public long length;
        public long alreadyFetchedOffset;
        public FetchDataCallback(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters) 
            : base(callbackInfo, callbackParameters)
        {
            this.offset = callbackParameters.FetchData.RequiredFileOffset;
            this.length = callbackParameters.FetchData.RequiredLength;
            this.alreadyFetchedOffset = 0;
        }
    }

    internal enum FetchDataType
    {
        FETCH_DATA,
        CANCEL_FETCH_DATA
    }

    internal class RunningTask
    {
        internal FetchDataType type;
        internal FetchDataCallback callback;
        internal Task task;
        internal CancellationTokenSource taskCancelation;
        internal string opID;

        internal RunningTask(FetchDataType type, FetchDataCallback callback, Task task, CancellationTokenSource taskCancelation, string opID)
        {
            this.type = type;
            this.callback = callback;
            this.task = task;
            this.taskCancelation = taskCancelation;
            this.opID = opID;
        }

        internal void Cancel()
        {
            taskCancelation.Cancel();
        }
    }
}
