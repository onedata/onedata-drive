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
    public class PlaceholderDataFetcher
    {
        private Logger logger;
        private LoggerFormater loggerFormater;
        public PlaceholderDataFetcher(Logger logger)
        {
            this.logger = logger;
            this.loggerFormater = new(logger);
        }
        public void FetchData(FetchDataCallback callback)
        {
            string opID = IdGenerator.GenerateId8();
            Func<CancellationToken, Task> func = (token) => FetchDataAsync(callback, token, opID);

            CloudSync.runningTasks.AddTask(
                func,
                TaskType.FETCH_DATA,
                callback,
                opID);
        }
        private async Task FetchDataAsync(FetchDataCallback callback, CancellationToken token, string opID)
        {
            List<string> moreInfo = new() {
                $"Path:     {callback.filePath}", 
                $"Offset:   {callback.offset}",
                $"Length:   {callback.length}",
                $"FileSize: {callback.fileSize}",
                $"OptLen:   {callback.optionalLength}",
                $"OptOfs:   {callback.optionalOffset}"};
            loggerFormater.LogFileOP(LogLevel.Info, "FETCH DATA", "START", moreInfo, callback.filePath, opID: opID);

            const long ALIGNMENT = 4096;

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
                    callback.fileSize - 1))
                {
                    const int CHUNK = (int)ALIGNMENT * 32;
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
                            token.ThrowIfCancellationRequested();
                            int receivedBytes = await stream.ReadAsync(buffer, read, CHUNK - read, token);
                            if (receivedBytes == 0)
                            {
                                Debug.Print("Fetch Data - End of stream");
                                throw new Exception("Fetch Data - End of stream");
                            }
                            read += receivedBytes;
                            livelinessChcecker.IamAlive();
                        } while (read < CHUNK && (read + offset) < (callback.fileSize));

                        Marshal.Copy(buffer, 0, unmanagedPointer, read);

                        td.Length = read;
                        td.Offset = offset;

                        offset += read;

                        op = CF_OPERATION_PARAMETERS.Create(td);

                        //CfReportProviderProgress(callback.connectionKey, callback.transferKey, callback.fileSize, offset);

                        HRESULT hres = CfExecute(oi, ref op);
                        if (hres != HRESULT.S_OK)
                        {
                            throw new Exception($"Fetch data CfExecute FAIL - HRES 0x{((uint)hres):X}: {hres}");
                        }
                        callback.alreadyFetchedOffset = offset;

                        token.ThrowIfCancellationRequested();
                    } while (offset < callback.fileSize);
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
                    ex = new Exception($"CfExecute Stop operation HRES 0x{((uint)hres):X}: {hres}", e);
                }

                loggerFormater.LogFileOP(LogLevel.Error, "FETCH DATA", "FAIL - No such file", ex, opID: opID);

                File.Delete(callback.filePath);
            }
            catch (OperationCanceledException)
             {
                td = new()
                {
                    CompletionStatus = new NTStatus((uint)CloudFilterEnum.STATUS_CLOUD_FILE_REQUEST_CANCELED),
                    Buffer = 0,
                    Offset = 0,
                    Length = 4096,
                    Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE
                };
                op = CF_OPERATION_PARAMETERS.Create(td);
                HRESULT hres = CfExecute(oi, ref op);
                if (hres != HRESULT.S_OK)
                {
                    Exception e = new Exception($"CfExecute Stop operation HRES 0x{((uint)hres):X}: {hres}");
                    loggerFormater.LogFileOP(LogLevel.Error, "FETCH DATA", "FAIL - Operation canceled", e, opID: opID);
                }
                else
                {
                    loggerFormater.LogFileOP(LogLevel.Info, "FETCH DATA", "OK - Operation canceled", opID: opID);
                }
            }
            catch (Exception e)
            {
                // NOTE: CompletionStatus = new NTStatus((uint)CloudFilterEnum.STATUS_CLOUD_FILE_REQUEST_ABORTED) - seems to be wrong
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
                    e = new Exception($"CfExecute Stop operation HRES 0x{((uint)hres):X}: {hres}", e);
                }
                loggerFormater.LogFileOP(LogLevel.Error, "FETCH DATA", "FAIL", e, opID: opID);
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedPointer);
            }
        }

        public void CancelFetchData(FetchDataCallback callback)
        {
            string opID = IdGenerator.GenerateId8();
            CancelFetchDataAsync(callback, opID);
        }

        private async void CancelFetchDataAsync(FetchDataCallback callback, string opID)
        {
            loggerFormater.LogFileOP(LogLevel.Info, "CANCEL FETCH DATA", "START", opID: opID);
            await Task.Run(() => { 
                long cancelStart = callback.offset;
                long cancelEnd = callback.offset + callback.length;

                List<RunningTask> terminateList = CloudSync.runningTasks.list.FindAll(
                    x => {
                        if (x.type == TaskType.FETCH_DATA)
                        {
                            FetchDataCallback? fetchDataCallback = x.callback as FetchDataCallback;
                            if (fetchDataCallback != null)
                            {
                                return fetchDataCallback.normalizedPath == callback.normalizedPath
                                    && fetchDataCallback.alreadyFetchedOffset >= cancelStart
                                    && (fetchDataCallback.offset + fetchDataCallback.length) <= cancelEnd;
                            }
                        }
                        return false;
                    });

                List<string> moreInfo = new() { "List:" };
                foreach (RunningTask task in terminateList)
                {
                    task.Cancel();
                    moreInfo.Add($"{task.opID}");
                }
                loggerFormater.LogFileOP(LogLevel.Info, "CANCEL FETCH DATA", "Canceled operations", opID: opID, moreInfo: moreInfo);
            });
            loggerFormater.LogFileOP(LogLevel.Info, "CANCEL FETCH DATA", "FINISHED", opID: opID);
        }
    }

    public class FetchDataCallback : Callback
    {
        public long offset;
        public long length;
        public long optionalOffset;
        public long optionalLength;
        public long alreadyFetchedOffset;
        public FetchDataCallback(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters) 
            : base(callbackInfo, callbackParameters)
        {
            this.offset = callbackParameters.FetchData.RequiredFileOffset;
            this.length = callbackParameters.FetchData.RequiredLength;
            this.optionalOffset = callbackParameters.FetchData.OptionalFileOffset;
            this.optionalLength = callbackParameters.FetchData.OptionalLength;
            this.alreadyFetchedOffset = 0;
        }
    }
}
