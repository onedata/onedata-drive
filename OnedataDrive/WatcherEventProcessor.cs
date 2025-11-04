using NLog;
using OnedataDrive.Interfaces;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.Kernel32.PSS_HANDLE_ENTRY;

namespace OnedataDrive
{
    internal class WatcherEventProcessor : EventProcessor<WatcherEvent>, IAddable<WatcherEvent>
    {
        protected Logger logger;
        protected LoggerFormater loggerFormater;
        public WatcherEventProcessor(Logger logger, int sleepInterval = 2000) : base(logger, "", sleepInterval)
        {
            this.logger = logger;
            this.loggerFormater = new(logger);
        }

        protected override bool ProcessEventWorker(Event<WatcherEvent> processedEvent, string opID)
        {
            if (PathUtils.IsSpacePath(processedEvent.@event.eventArgs.FullPath))
            {
                loggerFormater.LogFileOP(LogLevel.Info, "FILE CREATED", 
                    "IGNORED - spaces directory", filePath: processedEvent.@event.eventArgs.FullPath, opID: opID);
                return true;
            }
            // determine type
            if ((processedEvent.@event.eventArgs.ChangeType & WatcherChangeTypes.Created) is WatcherChangeTypes.Created)
            {
                // check if file is placeholder
                return Created(processedEvent.@event, opID);
                // convert to placeholder
                // push to cloud
            }

            if ((processedEvent.@event.eventArgs.ChangeType & WatcherChangeTypes.Changed) is WatcherChangeTypes.Changed)
            {
                // check if space path
                // check if file is placeholder
                // check if hydration is requested
                // check if in sync
                // push to cloud
                // set in sync
            }

            throw new NotImplementedException();
        }

        private bool Created(WatcherEvent @event, string opID)
        {
            if (!PathUtils.IsSpacePath(@event.eventArgs.FullPath))
            {
                loggerFormater.LogFileOP(LogLevel.Info, "FILE CREATED", "IGNORED - spaces directory", filePath: @event.eventArgs.FullPath, opID: opID);
                return true;
            }
            loggerFormater.LogFileOP(LogLevel.Info, "FILE CREATED", "START", filePath: @event.eventArgs.FullPath, opID: opID);

            try
            {
                RegisterFile(@event, opID);
                loggerFormater.LogFileOP(LogLevel.Info, "FILE CREATED", "FINISHED", filePath: @event.eventArgs.FullPath, opID: opID);
                return true;
            }
            catch (FileNotFoundException ex)
            {
                loggerFormater.LogFileOP(LogLevel.Error, "FILE CREATED", "FAILED - file not found", ex, filePath: @event.eventArgs.FullPath, opID: opID);
                return true;
            }
            catch (Exception ex)
            {
                loggerFormater.LogFileOP(LogLevel.Error, "FILE CREATED", "FAILED", ex, filePath: @event.eventArgs.FullPath, opID: opID);
                return false;
            }
        }

        private void RegisterFile(WatcherEvent @event, string opID = "")
        {
            string fullPath = @event.eventArgs.FullPath;

            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                throw new FileNotFoundException($"path: {fullPath}");
            }

            FileId id;

            FileAttributes attributes = File.GetAttributes(fullPath);
            bool isDir = (attributes & FileAttributes.Directory) == FileAttributes.Directory;

            if (isDir)
            {
                id = PushNewFolderToCloud(fullPath);
                loggerFormater.LogFileOP(LogLevel.Info, "RegisterFile", "Dir pushed to cloud", opID: opID);
            }
            else
            {
                id = PushNewFileToCloud(fullPath);
                loggerFormater.LogFileOP(LogLevel.Info, "RegisterFile", "File pushed to cloud", opID: opID);
            }

            try
            {
                ConvertToPlaceholder(fullPath, id, isDir);
                loggerFormater.LogFileOP(LogLevel.Info, "RegisterFile", "Converted to placeholder", opID: opID);
            }
            catch (Exception)
            {
                loggerFormater.LogFileOP(LogLevel.Error, "RegisterFile", "File was pushed to cloud - local file is NOT LINKED with cloud", opID: opID);
                throw;
            }
        }

        private FileId PushNewFolderToCloud(string fullPath)
        {
            CF_PLACEHOLDER_BASIC_INFO info = CldApiUtils.GetBasicInfo(PathUtils.GetParentPath(fullPath));

            string id = System.Text.Encoding.Unicode.GetString(info.FileIdentity);

            var task = RestClient.CreateFileInDir(
                    CloudSync.spaces[PathUtils.GetSpaceName(fullPath)].providerInfos,
                    id,
                    PathUtils.GetLastInPath(fullPath),
                    directory: true
                );
            task.Wait();
            return task.Result;
        }

        private FileId PushNewFileToCloud(string fullPath)
        {
            using (FileStream stream = File.OpenRead(fullPath))
            {
                CF_PLACEHOLDER_BASIC_INFO info = CldApiUtils.GetBasicInfo(PathUtils.GetParentPath(fullPath));

                string id = System.Text.Encoding.Unicode.GetString(info.FileIdentity);

                var task = RestClient.CreateFileInDir(
                    CloudSync.spaces[PathUtils.GetSpaceName(fullPath)].providerInfos,
                    id,
                    PathUtils.GetLastInPath(fullPath),
                    stream
                );
                task.Wait();
                return task.Result;
            }
        }

        private void ConvertToPlaceholder(string fullPath, FileId id, bool isDir = false)
        {
            SafeHCFFILE? protectedHandle = null;
            nint fileIdentity = IntPtr.Zero;
            try
            {
                HRESULT hresOpen = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, out protectedHandle);
                if (hresOpen != HRESULT.S_OK)
                {
                    throw new Exception("CfOpenFileWithOplock HRES: " + hresOpen);
                }

                fileIdentity = Marshal.StringToCoTaskMemUni(id.fileId);
                uint fileIdentityLength = (uint)id.fileId.Length * 2;

                HRESULT hresConvert;
                unsafe
                {
                    hresConvert = CfConvertToPlaceholder(protectedHandle.DangerousGetHandle(), fileIdentity, fileIdentityLength, CF_CONVERT_FLAGS.CF_CONVERT_FLAG_MARK_IN_SYNC);
                }
                if (hresConvert != HRESULT.S_OK)
                {
                    throw new Exception("CfConvertToPlaceholder HRES: " + hresConvert);
                }
            }
            finally
            {
                if (protectedHandle != null && !protectedHandle.IsInvalid)
                {
                    protectedHandle.Dispose();
                }
                if (fileIdentity != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(fileIdentity);
                }
            }

        }
    }


}
