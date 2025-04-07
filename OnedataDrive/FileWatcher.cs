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
            loggerFormater.Oneline(LogLevel.Info, "FILE CREATED", "START", e.FullPath, e.GetHashCode().ToString());

            // sleep is needed
            Thread.Sleep(1000);

            try
            {
                RegisterFile(e.FullPath, e.GetHashCode().ToString());
                loggerFormater.Oneline(LogLevel.Info, "FILE CREATED", "FINISHED", e.FullPath, e.GetHashCode().ToString());
            }
            catch (Exception ex)
            {
                loggerFormater.Oneline(LogLevel.Error, "FILE CREATED", "FAILED", ex, e.FullPath, e.GetHashCode().ToString());
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
            Debug.Print("FILE UPDATED(OnChanged): {0}", e.FullPath);

            if (!File.Exists(e.FullPath) && !Directory.Exists(e.FullPath))
            {
                Debug.Print("File does not exist: {0}", e.FullPath);
                return;
            }

            FileAttributes attributes = File.GetAttributes(e.FullPath);
            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                Debug.Print("File is DIR(nothing happens): {0}", e.FullPath);
                return;
            }

            try
            {
                CF_PLACEHOLDER_STANDARD_INFO info = CldApiUtils.GetStandardInfo(e.FullPath);

                UpdateFile(e, info);

                if (info.PinState == CF_PIN_STATE.CF_PIN_STATE_UNPINNED)
                {
                    Dehydrate(e.FullPath, info);
                }
                if (info.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED)
                {
                    Hydrate(e.FullPath, info);
                }
            }
            catch (Exception exception)
            {
                Debug.Print(exception.ToString());
                Debug.Print("FileWatcher OnChanged FAIL");
            }

            Debug.Print("");
        }

        private void Hydrate(string fullPath, CF_PLACEHOLDER_STANDARD_INFO info)
        {
            try
            {
                Debug.Print("PINNED -> Hydrate {0}", fullPath);
                HRESULT hresOpen = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS, out SafeHCFFILE protectedHandle);
                HRESULT hresHydrate = CfHydratePlaceholder(protectedHandle.DangerousGetHandle());
                CfCloseHandle(protectedHandle);

                if (hresOpen != HRESULT.S_OK || hresHydrate != HRESULT.S_OK)
                {
                    throw new Exception("Open: " + hresOpen + ", Hydrate: " + hresHydrate);
                }
            }
            catch (Exception e)
            {
                Debug.Print(e.ToString());
                Debug.Print("Hydrate FAIL");
            }
            Debug.Print("Hydrate OK");
        }

        private void Dehydrate(string fullPath, CF_PLACEHOLDER_STANDARD_INFO info)
        {
            try
            {
                Debug.Print("UNPINNED -> Dehydrate {0}", fullPath);
                HRESULT hresOpen = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS, out SafeHCFFILE protectedHandle);
                HRESULT hresDehydrate = CfDehydratePlaceholder(protectedHandle.DangerousGetHandle(), 0, info.OnDiskDataSize, CF_DEHYDRATE_FLAGS.CF_DEHYDRATE_FLAG_NONE);
                HRESULT hresPinState = CfSetPinState(protectedHandle.DangerousGetHandle(), CF_PIN_STATE.CF_PIN_STATE_UNSPECIFIED, CF_SET_PIN_FLAGS.CF_SET_PIN_FLAG_NONE);
                CfCloseHandle(protectedHandle);

                if (hresOpen != HRESULT.S_OK || hresDehydrate != HRESULT.S_OK || hresPinState != HRESULT.S_OK)
                {
                    throw new Exception("open: " + hresOpen + ", dehydrate: " + hresDehydrate + ", pin state: " + hresPinState);
                }
            }
            catch (Exception e)
            {
                Debug.Print(e.ToString());
                Debug.Print("Dehydrate FAIL");
            }
            Debug.Print("Dehydrate OK");
        }

        private void UpdateFile(FileSystemEventArgs e, CF_PLACEHOLDER_STANDARD_INFO info)
        {
            // test if file/folder is in sync. If true -> finish
            if (info.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC)
            {
                Debug.Print("File is in sync (OnChanged END)");
                return;
            }

            try
            {
                PushToCloudUpdate(e.FullPath, info);
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException is NoSuchCloudFile)
                {
                    Debug.Print(ae.ToString());
                    File.Delete(e.FullPath);
                    Debug.Print("File was deleted, because it does not exist on cloud");
                    return;
                }
                else
                {
                    throw;
                }

            }
            catch (Exception exception)
            {
                Debug.Print(exception.ToString());
                Debug.Print("FAILED to sync {0}", e.FullPath);
                return;
            }

            try
            {
                // TODO (done)
                CfOpenFileWithOplock(e.FullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS | CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, out SafeHCFFILE handle);
                // SetInSyncState and set metadata ;
                UpdatePlaceholderMetadata(info, handle, e.FullPath);
                CfCloseHandle(handle);
                Debug.Print("File sync OK: {0}", e.FullPath);
            }
            catch (Exception exception)
            {
                Debug.Print(exception.ToString());
                Debug.Print("FAIL {0} : File was uploaded to cloud, however local file isn't linked with cloud", e.FullPath);
                return;
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
                throw new Exception("UpdatePlaceholderMetadata - CfUpdatePlaceholder: " + hres);
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