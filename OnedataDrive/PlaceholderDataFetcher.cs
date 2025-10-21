using NLog;
using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive
{
    public class PlaceholderDataFetcher
    {
        internal List<RunningTask> runningFetch;
        private CancellationTokenSource masterTokenSource;
        public PlaceholderDataFetcher()
        {
            this.runningFetch = new();
            this.masterTokenSource = new();
        }
        public void FetchData(FetchDataCallback callback)
        {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(masterTokenSource.Token);
            Task fetchTask = FetchDataAsync(callback, cts.Token);
        }
        private async Task FetchDataAsync(FetchDataCallback callback, CancellationToken token)
        {
            string opID = IdGenerator.GenerateId8();

            CloudProvider.PrintInfo(callback, LogLevel.Info, "FETCH DATA", "START", opID: opID);

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

                var taskInfo = RestClient.GetFileAttribute(fileIdentity, space.providerInfos, token);
                taskInfo.Wait();
                FileAttribute fileInfo = taskInfo.Result;
                if (fileInfo.size != callback.fileSize)
                {
                    throw new Exception("Size of cloud file does not match local file size. Try to refresh placeholders (R)");
                    // TODO: update placeholder, so operation runs OK
                }

                Task<Stream> taskData = RestClient.GetStream(
                    CloudSync.spaces[PathUtils.GetSpaceName(callback.filePath)].providerInfos,
                    callback.fileIdentity
                    );
                taskData.Wait();
                //////////////
                using (LivelinessChcecker livelinessChcecker = new(10 * 1000, turnOffWhenDead: false))
                using (Stream stream = taskData.Result)
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
                        } while (read < CHUNK && (read + offset) < (callback.offset + callback.length));

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
                    } while (offset < (callback.offset + callback.length));
                }
                CloudProvider.PrintInfo(callback, LogLevel.Info, "FETCH DATA", "OK", opID:opID);
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

                CloudProvider.PrintInfo(callback, LogLevel.Warn, "FETCH DATA", "FAIL - No such file", ex, opID:opID);

                File.Delete(callback.filePath);
            }
            catch (Exception e)
            {
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

                CloudProvider.PrintInfo(callback, LogLevel.Error, "FETCH DATA", "FAIL", e, opID: opID);
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedPointer);
            }
        }

        public void CancelFetchData(FetchDataCallback callback)
        {

        }
    }

    public class FetchDataCallback : Callback
    {
        public long offset;
        public long length;
        public FetchDataCallback(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters) 
            : base(callbackInfo, callbackParameters)
        {
            this.offset = callbackParameters.FetchData.RequiredFileOffset;
            this.length = callbackParameters.FetchData.RequiredLength;
        }
    }

    internal class RunningTask
    {
        internal FetchDataCallback callback;
        internal Task task;
        internal CancellationTokenSource taskCancelation;

        internal RunningTask(FetchDataCallback callback, Task task, CancellationTokenSource taskCancelation)
        {
            this.callback = callback;
            this.task = task;
            this.taskCancelation = taskCancelation;
        }
    }
}
