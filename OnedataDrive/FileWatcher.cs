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
    public class FileWatcher
    {
        private FileSystemWatcher watcher;
        private bool disposed = true;
        private Logger logger;
        private LoggerFormater loggerFormater;

        public FileWatcher()
        {
            this.watcher = new();
            this.disposed = false;
            this.logger = LogManager.GetCurrentClassLogger();
            this.loggerFormater = new(logger);
        }

        public FileWatcher(string rootDir)
        {
            this.watcher = new(rootDir);
            this.logger = LogManager.GetCurrentClassLogger();
            this.loggerFormater = new(logger);

            this.watcher.NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastWrite;


            this.watcher.Created += new FileSystemEventHandler(OnCreated);
            this.watcher.Changed += new FileSystemEventHandler(OnChanged);
            this.watcher.Error += new ErrorEventHandler(OnError);

            this.watcher.IncludeSubdirectories = true;
            this.watcher.EnableRaisingEvents = true;

            this.disposed = false;
        }

        public void OnError(object sender, ErrorEventArgs e)
        {
            // TODO: do something clever
            logger.Error("Filewatcher FAILED", e.GetException());
        }

        public void OnCreated(object sender, FileSystemEventArgs e)
        {
            string opID = e.GetHashCode().ToString();
            loggerFormater.Oneline(LogLevel.Info, "FILE CREATED", "START", filePath: e.FullPath, opID: opID);

            // sleep is needed
            Thread.Sleep(1000);

            try
            {
                RegisterFile(e.FullPath, opID);
                loggerFormater.Oneline(LogLevel.Info, "FILE CREATED", "FINISHED", filePath: e.FullPath, opID: opID);
            }
            catch (Exception ex)
            {
                loggerFormater.Oneline(LogLevel.Error, "FILE CREATED", "FAILED", ex, filePath: e.FullPath, opID: opID);
            }
            
        }

        private void RegisterFile(string fullPath, string opID = "")
        {
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                throw new FileNotFoundException($"path: {fullPath}");
            }

            FileId id;

            FileAttributes attributes = File.GetAttributes(fullPath);
            bool isDir = (attributes & FileAttributes.Directory) == FileAttributes.Directory;

            if (isDir)
            {
                loggerFormater.Oneline(LogLevel.Debug, "RegisterFile", "file is DIR", opID: opID);
                id = PushNewFolderToCloud(fullPath);
            }
            else
            {
                loggerFormater.Oneline(LogLevel.Debug, "RegisterFile", "file is REG", opID: opID);
                id = PushNewFileToCloud(fullPath);
            }

            try
            {
                ConvertToPlaceholder(fullPath, id, isDir);
            }
            catch (Exception)
            {
                loggerFormater.Oneline(LogLevel.Error, "RegisterFile", "File was pushed to cloud - local file is NOT LINKED with cloud", opID: opID);
                throw;
            }
        }

        public void OnChanged(object sender, FileSystemEventArgs e)
        {
            string opID = e.GetHashCode().ToString();
            try
            {
                loggerFormater.Oneline(LogLevel.Info, "FILE CHANGED", "START", filePath: e.FullPath, opID: opID);

                if (!File.Exists(e.FullPath) && !Directory.Exists(e.FullPath))
                {
                    loggerFormater.Oneline(LogLevel.Warn, "FILE CHANGED", "FINISHED - File not found", filePath: e.FullPath, opID: opID);
                    return;
                }

                FileAttributes attributes = File.GetAttributes(e.FullPath);
                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    loggerFormater.Oneline(LogLevel.Warn, "FILE CHANGED", "FINISHED - File is DIR", filePath: e.FullPath, opID: opID);
                    return;
                }

                CF_PLACEHOLDER_STANDARD_INFO info = CldApiUtils.GetStandardInfo(e.FullPath);

                UpdateFile(e, info, opID);

                if (info.PinState == CF_PIN_STATE.CF_PIN_STATE_UNPINNED)
                {
                    Dehydrate(e.FullPath, info, opID);
                }
                if (info.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED)
                {
                    Hydrate(e.FullPath, info, opID);
                }
            }
            catch (Exception exception)
            {
                loggerFormater.Oneline(LogLevel.Error, "FILE CHANGED", "FAILED", exception, filePath: e.FullPath, opID: opID);
            }

            loggerFormater.Oneline(LogLevel.Info, "FILE CHANGED", "FINISHED", filePath: e.FullPath, opID: opID);
        }

        private void Hydrate(string fullPath, CF_PLACEHOLDER_STANDARD_INFO info, string opID = "")
        {
            //Debug.Print("PINNED -> Hydrate {0}", fullPath);
            loggerFormater.Oneline(LogLevel.Debug, "Hydrate", "START", filePath: fullPath, opID: opID);

            HRESULT hresOpen = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS, out SafeHCFFILE protectedHandle);
            HRESULT hresHydrate = CfHydratePlaceholder(protectedHandle.DangerousGetHandle());
            CfCloseHandle(protectedHandle);

            if (hresOpen != HRESULT.S_OK || hresHydrate != HRESULT.S_OK)
            {
                throw new Exception("CfOpenFileWithOplock: " + hresOpen + ", CfHydratePlaceholder: " + hresHydrate);
            }

            loggerFormater.Oneline(LogLevel.Debug, "Hydrate", "FINISHED", opID: opID);
        }

        private void Dehydrate(string fullPath, CF_PLACEHOLDER_STANDARD_INFO info, string opID = "")
        {
            loggerFormater.Oneline(LogLevel.Debug, "Dehydrate", "START", filePath: fullPath, opID: opID);

            HRESULT hresOpen = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS, out SafeHCFFILE protectedHandle);
            HRESULT hresDehydrate = CfDehydratePlaceholder(protectedHandle.DangerousGetHandle(), 0, info.OnDiskDataSize, CF_DEHYDRATE_FLAGS.CF_DEHYDRATE_FLAG_NONE);
            HRESULT hresPinState = CfSetPinState(protectedHandle.DangerousGetHandle(), CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED, CF_SET_PIN_FLAGS.CF_SET_PIN_FLAG_NONE);
            CfCloseHandle(protectedHandle);

            if (hresOpen != HRESULT.S_OK || hresDehydrate != HRESULT.S_OK || hresPinState != HRESULT.S_OK)
            {
                throw new Exception("CfOpenFileWithOplock: " + hresOpen + ", CfDehydratePlaceholder: " + hresDehydrate + ", CfSetPinState: " + hresPinState);
            }

            loggerFormater.Oneline(LogLevel.Debug, "Dehydrate", "FINISHED", opID: opID);
        }

        private void UpdateFile(FileSystemEventArgs e, CF_PLACEHOLDER_STANDARD_INFO info, string opID = "")
        {
            // test if file/folder is in sync. If true -> finish
            if (info.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
            {
                loggerFormater.Oneline(LogLevel.Info, "UpdateFile", "File was already in sync", opID: opID);
                return;
            }

            try
            {
                PushToCloudUpdate(e.FullPath, info);
            }
            catch (AggregateException ae) when (ae.InnerException is NoSuchCloudFile)
            {
                File.Delete(e.FullPath);
                loggerFormater.Oneline(LogLevel.Info, "UpdateFile", "Coresponding cloud file does not exist. Local file was deleted", ae, opID: opID);
                return;

            }

            SafeHCFFILE? handle = null;
            try
            {
                HRESULT openHres = CfOpenFileWithOplock(e.FullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS | CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, out handle);
                if (openHres != HRESULT.S_OK)
                {
                    throw new Exception("CfOpenFileWithOplock HRES: " + openHres);
                }
                // SetInSyncState and set metadata
                UpdatePlaceholderMetadata(info, handle, e.FullPath);
            }
            catch (Exception)
            {
                loggerFormater.Oneline(LogLevel.Error, "UpdateFile", "File was uploaded to cloud, however local file isn't linked with cloud", opID: opID);
                throw;
            }
            finally
            {
                if (handle != null)
                {
                    CfCloseHandle(handle);
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

        private void ConvertToPlaceholder(string fullPath, FileId id, bool isDir = false)
        {
            HRESULT hresult;

            SafeHCFFILE protectedHandle;
            hresult = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, out protectedHandle);
            if (hresult != HRESULT.S_OK)
            {
                throw new Exception("CfOpenFileWithOplock HRES: " + hresult);
            }

            nint fileIdentity = Marshal.StringToCoTaskMemUni(id.fileId);
            uint fileIdentityLength = (uint)id.fileId.Length * 2;

            unsafe
            {
                hresult = CfConvertToPlaceholder(protectedHandle.DangerousGetHandle(), fileIdentity, fileIdentityLength, CF_CONVERT_FLAGS.CF_CONVERT_FLAG_MARK_IN_SYNC);
            }
            CfCloseHandle(protectedHandle);
            Marshal.FreeCoTaskMem(fileIdentity);
            if (hresult != HRESULT.S_OK)
            {
                throw new Exception("CfConvertToPlaceholder HRES: " + hresult);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                disposed = true;
            }
        }

        public void Pause()
        {
            watcher.EnableRaisingEvents = false;
        }

        public void Resume()
        {
            watcher.EnableRaisingEvents = true;
        }

    }
}