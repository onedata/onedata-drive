using NLog;
using OnedataDrive.ErrorHandling;
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
            if (!PathUtils.IsSpacePath(processedEvent.@event.eventArgs.FullPath))
            {
                loggerFormater.LogFileOP(LogLevel.Info, "FILE CREATED", 
                    "IGNORED - spaces directory", filePath: processedEvent.@event.eventArgs.FullPath, opID: opID);
                return true;
            }
            // determine type
            if ((processedEvent.@event.eventArgs.ChangeType & WatcherChangeTypes.Created) is WatcherChangeTypes.Created)
            {
                return Created(processedEvent.@event, opID);
                // check if file is placeholder
                // convert to placeholder
                // push to cloud
            }

            if ((processedEvent.@event.eventArgs.ChangeType & WatcherChangeTypes.Changed) is WatcherChangeTypes.Changed)
            {
                return Changed(processedEvent.@event, opID);
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

        private bool Changed(WatcherEvent @event, string opID)
        {
            loggerFormater.LogFileOP(LogLevel.Info, "FILE CHANGED", "START", filePath: @event.eventArgs.FullPath, opID: opID);
            try
            {
                if (!File.Exists(@event.eventArgs.FullPath) && !Directory.Exists(@event.eventArgs.FullPath))
                {
                    loggerFormater.LogFileOP(LogLevel.Warn, "FILE CHANGED", "FINISHED - File not found", filePath: @event.eventArgs.FullPath, opID: opID);
                    return true;
                }

                FileAttributes attributes = File.GetAttributes(@event.eventArgs.FullPath);
                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    loggerFormater.LogFileOP(LogLevel.Info, "FILE CHANGED", "FINISHED - File is DIR", filePath: @event.eventArgs.FullPath, opID: opID);
                    return true;
                }

                CF_PLACEHOLDER_STANDARD_INFO info = CldApiUtils.GetStandardInfo(@event.eventArgs.FullPath);

                UpdateFile(@event.eventArgs, info, opID);

                if (info.PinState == CF_PIN_STATE.CF_PIN_STATE_UNPINNED)
                {
                    Dehydrate(@event.eventArgs.FullPath, info, opID);
                }
                if (info.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED)
                {
                    Hydrate(@event.eventArgs.FullPath, info, opID);
                }
            }
            catch (Exception exception)
            {
                loggerFormater.LogFileOP(LogLevel.Error, "FILE CHANGED", "FAILED", exception, filePath: @event.eventArgs.FullPath, opID: opID);
                return false;
            }

            loggerFormater.LogFileOP(LogLevel.Info, "FILE CHANGED", "FINISHED", filePath: @event.eventArgs.FullPath, opID: opID);
            return true;
        }

        private void Hydrate(string fullPath, CF_PLACEHOLDER_STANDARD_INFO info, string opID = "")
        {
            SafeHCFFILE? protectedHandle = null;
            try
            {
                HRESULT hresOpen = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS, out protectedHandle);
                HRESULT hresHydrate = CfHydratePlaceholder(protectedHandle.DangerousGetHandle());

                if (hresOpen != HRESULT.S_OK || hresHydrate != HRESULT.S_OK)
                {
                    throw new Exception("CfOpenFileWithOplock: " + hresOpen + ", CfHydratePlaceholder: " + hresHydrate);
                }

                loggerFormater.LogFileOP(LogLevel.Info, "Hydrate", "OK", opID: opID);
            }
            finally
            {
                if (protectedHandle != null && !protectedHandle.IsInvalid)
                {
                    protectedHandle.Dispose();
                }
            }

        }

        private void Dehydrate(string fullPath, CF_PLACEHOLDER_STANDARD_INFO info, string opID = "")
        {
            SafeHCFFILE? protectedHandle = null;
            try
            {
                HRESULT hresOpen = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS, out protectedHandle);
                HRESULT hresDehydrate = CfDehydratePlaceholder(protectedHandle.DangerousGetHandle(), 0, info.OnDiskDataSize, CF_DEHYDRATE_FLAGS.CF_DEHYDRATE_FLAG_NONE);
                HRESULT hresPinState = CfSetPinState(protectedHandle.DangerousGetHandle(), CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED, CF_SET_PIN_FLAGS.CF_SET_PIN_FLAG_NONE);

                if (hresOpen != HRESULT.S_OK || hresDehydrate != HRESULT.S_OK || hresPinState != HRESULT.S_OK)
                {
                    throw new Exception("CfOpenFileWithOplock: " + hresOpen + ", CfDehydratePlaceholder: " + hresDehydrate + ", CfSetPinState: " + hresPinState);
                }

                loggerFormater.LogFileOP(LogLevel.Info, "Dehydrate", "OK", opID: opID);
            }
            finally
            {
                if (protectedHandle != null && !protectedHandle.IsInvalid)
                {
                    protectedHandle.Dispose();
                }
            }

        }

        private void UpdateFile(FileSystemEventArgs e, CF_PLACEHOLDER_STANDARD_INFO info, string opID = "")
        {
            // test if file/folder is in sync. If true -> finish
            if (info.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
            {
                loggerFormater.LogFileOP(LogLevel.Info, "UpdateFile", "File was already in sync", opID: opID);
                return;
            }

            try
            {
                PushToCloudUpdate(e.FullPath, info);
                loggerFormater.LogFileOP(LogLevel.Info, "PushToCloudUpdate", "OK", opID: opID);
            }
            catch (AggregateException ae) when (ae.InnerException is NoSuchCloudFile)
            {
                File.Delete(e.FullPath);
                loggerFormater.LogFileOP(LogLevel.Info, "UpdateFile", "Coresponding cloud file does not exist. Local file was deleted", ae, opID: opID);
                return;

            }

            SafeHCFFILE? protectedHandle = null;
            try
            {
                HRESULT openHres = CfOpenFileWithOplock(e.FullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS | CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, out protectedHandle);
                if (openHres != HRESULT.S_OK)
                {
                    throw new Exception("CfOpenFileWithOplock HRES: " + openHres);
                }
                // SetInSyncState and set metadata
                UpdatePlaceholderMetadata(info, protectedHandle, e.FullPath);
                loggerFormater.LogFileOP(LogLevel.Info, "UpdatePlaceholderMetadata", "OK", opID: opID);
            }
            catch (Exception)
            {
                loggerFormater.LogFileOP(LogLevel.Error, "UpdateFile", "File was uploaded to cloud, however local file isn't linked with cloud", opID: opID);
                throw;
            }
            finally
            {
                if (protectedHandle != null && !protectedHandle.IsInvalid)
                {
                    protectedHandle.Dispose();
                }
            }
        }

        private void UpdatePlaceholderMetadata(CF_PLACEHOLDER_STANDARD_INFO info, SafeHCFFILE handle, string path)
        {
            string id = System.Text.Encoding.Unicode.GetString(info.FileIdentity);

            var task = RestClient.GetFileAttribute(
                id,
                CloudSync.spaces[PathUtils.GetSpaceName(path)].providerInfos
            );
            task.Wait();
            FileAttribute attribute = task.Result;

            CF_FS_METADATA metadata = Placeholders.CreateFSMetadata(attribute);
            //metadata.FileSize = info.PropertiesSize;

            long updateUsn = 0;
            HRESULT hres = CfUpdatePlaceholder(FileHandle: handle.DangerousGetHandle(),
                                FsMetadata: metadata,
                                FileIdentity: 0,
                                FileIdentityLength: 0,
                                DehydrateRangeCount: 0,
                                UpdateFlags: CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC,
                                UpdateUsn: ref updateUsn
                                );
            if (hres != HRESULT.S_OK)
            {
                throw new Exception("CfUpdatePlaceholder HRES: " + hres);
            }
        }

        private void PushToCloudUpdate(string fullPath, CF_PLACEHOLDER_STANDARD_INFO info)
        {
            string id = System.Text.Encoding.Unicode.GetString(info.FileIdentity);

            using (FileStream stream = File.OpenRead(fullPath))
            {
                var task = RestClient.PostFileContent(
                    CloudSync.spaces[PathUtils.GetSpaceName(fullPath)].providerInfos,
                    id,
                    stream
                );
                task.Wait();
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
