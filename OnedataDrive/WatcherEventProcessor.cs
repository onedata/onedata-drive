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

        protected override bool ProcessEventWorker(EventPenalizable<WatcherEvent> processedEvent)
        {
            if (!Path.Exists(processedEvent.@event.eventArgs.FullPath))
            {
                loggerFormater.LogFileOP(LogLevel.Warn, "FILEWATCHER EVENT", 
                    "IGNORED - file/dir does not exists", filePath: processedEvent.@event.eventArgs.FullPath, opID: processedEvent.@event.eventId);
                return true;
            }

            if (!PathUtils.IsSpacePath(processedEvent.@event.eventArgs.FullPath))
            {
                loggerFormater.LogFileOP(LogLevel.Info, "FILEWATCHER EVENT", 
                    "IGNORED - spaces directory", filePath: processedEvent.@event.eventArgs.FullPath, opID: processedEvent.@event.eventId);
                return true;
            }

            CF_PLACEHOLDER_STANDARD_INFO info;
            try
            {
                info = CldApiUtils.GetStandardInfo(processedEvent.@event.eventArgs.FullPath);
            }
            catch (NotPlaceholder)
            {
                return Created(processedEvent.@event);
            }
            catch (Exception e)
            {
                loggerFormater.LogFileOP(LogLevel.Error, "FILEWATCHER EVENT", 
                    "FAILED to get placeholder info", e, filePath: processedEvent.@event.eventArgs.FullPath, opID: processedEvent.@event.eventId);
                return false;
            }

            return Changed(processedEvent.@event, info);
        }


        private bool Created(WatcherEvent @event)
        {
            loggerFormater.LogFileOP(LogLevel.Info, "FILE CREATED", "START", filePath: @event.eventArgs.FullPath, opID: @event.eventId);

            if (CloudSync.configuration.readOnly)
            {
                loggerFormater.LogFileOP(LogLevel.Info, "FILE CREATED", "IGNORED - read-only mode", filePath: @event.eventArgs.FullPath, opID: @event.eventId);
                return true;
            }

            try
            {
                RegisterFile(@event, @event.eventId);
                loggerFormater.LogFileOP(LogLevel.Info, "FILE CREATED", "FINISHED", filePath: @event.eventArgs.FullPath, opID: @event.eventId);
                return true;
            }
            catch (Exception ex)
            {
                loggerFormater.LogFileOP(LogLevel.Error, "FILE CREATED", "FAILED", ex, filePath: @event.eventArgs.FullPath, opID: @event.eventId);
                return false;
            }
        }

        private bool Changed(WatcherEvent @event, CF_PLACEHOLDER_STANDARD_INFO info)
        {
            loggerFormater.LogFileOP(LogLevel.Info, "FILE CHANGED", "START", filePath: @event.eventArgs.FullPath, opID: @event.eventId);

            try
            {
                FileAttributes attributes = File.GetAttributes(@event.eventArgs.FullPath);
                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    loggerFormater.LogFileOP(LogLevel.Info, "FILE CHANGED", "FINISHED - File is DIR", filePath: @event.eventArgs.FullPath, opID: @event.eventId);
                    return true;
                }

                if (info.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC)
                {
                    UpdateCloudFile(@event.eventArgs, info, @event.eventId);
                }

                if (info.PinState == CF_PIN_STATE.CF_PIN_STATE_UNPINNED)
                {
                    Dehydrate(@event.eventArgs.FullPath, info, @event.eventId);
                }
                if (info.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED)
                {
                    Hydrate(@event.eventArgs.FullPath, info, @event.eventId);
                }
            }
            catch (Exception exception)
            {
                loggerFormater.LogFileOP(LogLevel.Error, "FILE CHANGED", "FAILED", exception, filePath: @event.eventArgs.FullPath, opID: @event.eventId);
                return false;
            }

            loggerFormater.LogFileOP(LogLevel.Info, "FILE CHANGED", "FINISHED", filePath: @event.eventArgs.FullPath, opID: @event.eventId);
            return true;
        }

        private void Hydrate(string fullPath, CF_PLACEHOLDER_STANDARD_INFO info, string opID = "")
        {
            using CfHandle handle = new(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS);
            HRESULT hresHydrate = CfHydratePlaceholder(handle.GetDangerousHandle());

            if (hresHydrate != HRESULT.S_OK)
            {
                throw new Exception("CfHydratePlaceholder: " + hresHydrate);
            }

            loggerFormater.LogFileOP(LogLevel.Info, "Hydrate", "OK", opID: opID);
        }

        private void Dehydrate(string fullPath, CF_PLACEHOLDER_STANDARD_INFO info, string opID = "")
        {
            using CfHandle handle = new(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS);
            HRESULT hresDehydrate = CfDehydratePlaceholder(handle.GetDangerousHandle(), 0, info.OnDiskDataSize, CF_DEHYDRATE_FLAGS.CF_DEHYDRATE_FLAG_NONE);
            HRESULT hresPinState = CfSetPinState(handle.GetDangerousHandle(), CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED, CF_SET_PIN_FLAGS.CF_SET_PIN_FLAG_NONE);

            if (hresDehydrate != HRESULT.S_OK || hresPinState != HRESULT.S_OK)
            {
                throw new Exception("CfDehydratePlaceholder: " + hresDehydrate + ", CfSetPinState: " + hresPinState);
            }

            loggerFormater.LogFileOP(LogLevel.Info, "Dehydrate", "OK", opID: opID);
        }

        private void UpdateCloudFile(FileSystemEventArgs e, CF_PLACEHOLDER_STANDARD_INFO info, string opID = "")
        {
            if (CloudSync.configuration.readOnly)
            {
                loggerFormater.LogFileOP(LogLevel.Info, "FILE CHANGED", "IGNORED - read-only mode", opID: opID);
                return;
            }

            try
            {
                PushToCloudUpdate(e.FullPath, info);
                loggerFormater.LogFileOP(LogLevel.Info, "PushToCloudUpdate", "OK", opID: opID);
            }
            catch (AggregateException ae) when (ae.InnerException is NoSuchCloudFile)
            {
                // upload file again
                // convert to regular file
                // File.Delete(e.FullPath);
                loggerFormater.LogFileOP(LogLevel.Info, "UpdateFile", "Coresponding cloud file does not exist", ae, opID: opID);
                return;

            }

            try
            {
                CF_OPEN_FILE_FLAGS openFlags = CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS | CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE;
                using CfHandle handle = new(e.FullPath, openFlags);
                
                // SetInSyncState and set metadata
                UpdatePlaceholderMetadata(info, handle.GetSafeHandle(), e.FullPath);
                loggerFormater.LogFileOP(LogLevel.Info, "UpdatePlaceholderMetadata", "OK", opID: opID);
            }
            catch (Exception)
            {
                loggerFormater.LogFileOP(LogLevel.Error, "UpdateFile", "File was uploaded to cloud, however local file isn't linked with cloud", opID: opID);
                throw;
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

            CF_FS_METADATA metadata = PlaceholderData.CreateFSMetadata(attribute);
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
            string fileId = System.Text.Encoding.Unicode.GetString(info.FileIdentity);
            List<ProviderInfo> providers = CloudSync.spaces[PathUtils.GetSpaceName(fullPath)].providerInfos;

            PushToCloudUpdate(fullPath, fileId, providers);
        }

        private void PushToCloudUpdate(string fullPath, string fileId, List<ProviderInfo> providers)
        {
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                var task = RestClient.PostFileContent(
                    providers,
                    fileId,
                    stream
                );
                task.Wait();
            }
        }

        private void RegisterFile(WatcherEvent @event, string opID = "")
        {
            string fullPath = @event.eventArgs.FullPath;

            string parentPath = PathUtils.GetParentPath(fullPath);
            CF_PLACEHOLDER_BASIC_INFO parentInfo = CldApiUtils.GetBasicInfo(parentPath);
            string parentId = System.Text.Encoding.Unicode.GetString(parentInfo.FileIdentity);
            SpaceFolder spaceFolder = CloudSync.spaces[PathUtils.GetSpaceName(fullPath)];
            List<ProviderInfo> providers = spaceFolder.providerInfos;

            FileId id;

            FileAttributes attributes = File.GetAttributes(fullPath);
            bool isDir = (attributes & FileAttributes.Directory) == FileAttributes.Directory;

            // push empty file/folder to cloud
            id = CreateCloudEntry(fullPath, parentId, providers, directory: isDir);
            loggerFormater.LogFileOP(LogLevel.Info, "RegisterFile", "Created empty cloud entry", opID: opID);

            // register it as placeholder - not in sync
            try
            {
                string msg = "Converted to empty placeholder";
                if (isDir)
                {
                    spaceFolder.autoRefresh?.AddToMonitored(id.fileId, fullPath);
                    msg += " and added to auto refresh";
                }
                ConvertToPlaceholder(fullPath, id, isDir, setInSync: false);
                loggerFormater.LogFileOP(LogLevel.Info, "RegisterFile", msg, opID: opID);
            }
            catch (Exception e)
            {
                if (isDir)
                {
                    spaceFolder.autoRefresh?.RemoveFromMonitored(id.fileId);
                }
                loggerFormater.LogFileOP(LogLevel.Error, "RegisterFile", "Can not convert to placeholder", e, opID: opID);
                RestClient.Delete(providers, id.fileId).Wait();
                throw;
            }

            // push data to cloud
            if (!isDir)
            {
                try
                {
                    PushToCloudUpdate(fullPath, id.fileId, providers);
                }
                catch (Exception e)
                {
                    loggerFormater.LogFileOP(LogLevel.Error, "RegisterFile", "Can not push file content to cloud", e, opID: opID);
                    throw;
                }
            }

            // set in sync
            try
            {
                CldApiUtils.SetInSyncState(fullPath);
            }
            catch (Exception e)
            {
                logFormatter.LogFileOP(LogLevel.Error, "RegisterFile", "Can not set file as in sync", e, opID: opID);
                throw;
            }
        }

        private FileId CreateCloudEntry(string fullPath, string parentId, List<ProviderInfo> providers, bool directory)
        {
            var task = RestClient.CreateFileInDir(
                    providers,
                    parentId,
                    PathUtils.GetLastInPath(fullPath),
                    directory: directory
                );
            task.Wait();
            return task.Result;
        }

        private void ConvertToPlaceholder(string fullPath, FileId id, bool isDir = false, bool setInSync = true)
        {
            nint fileIdentity = IntPtr.Zero;
            try
            {
                CF_OPEN_FILE_FLAGS openFlags = CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_FOREGROUND
                | CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE;
                using CfHandle handle = new(fullPath, openFlags);

                fileIdentity = Marshal.StringToCoTaskMemUni(id.fileId);
                uint fileIdentityLength = (uint)id.fileId.Length * 2;

                HRESULT hresConvert;
                CF_CONVERT_FLAGS inSyncFlags =
                    setInSync ? CF_CONVERT_FLAGS.CF_CONVERT_FLAG_MARK_IN_SYNC : CF_CONVERT_FLAGS.CF_CONVERT_FLAG_NONE;
                unsafe
                {
                    hresConvert = CfConvertToPlaceholder(handle.GetDangerousHandle(),
                        fileIdentity, fileIdentityLength, inSyncFlags);
                }
                if (hresConvert != HRESULT.S_OK)
                {
                    throw new Exception("CfConvertToPlaceholder HRES: " + hresConvert);
                }
            }
            finally
            {
                if (fileIdentity != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(fileIdentity);
                }
            }

        }
    }
}
