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

        public FileWatcher()
        {
            watcher = new();
            disposed = false;
        }

        public FileWatcher(string rootDir)
        {
            watcher = new(rootDir);

            watcher.NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     //  | NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite;
            //  | NotifyFilters.Security
            //  | NotifyFilters.Size;

            watcher.Created += new FileSystemEventHandler(OnCreated);
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Error += new ErrorEventHandler(OnError);

            watcher.IncludeSubdirectories = true;

            watcher.EnableRaisingEvents = true;
            disposed = false;
        }

        public void OnError(object sender, ErrorEventArgs e)
        {
            // TODO: do something clever
            Debug.Print("OnError: " + e.GetException().Message);
        }

        public void OnCreated(object sender, FileSystemEventArgs e)
        {
            Debug.Print("NEW FILE DETECTED: {0}", e.FullPath);
            // sleep is needed
            Thread.Sleep(1000);

            RegisterFile(e.FullPath);
            Debug.Print("");
        }

        private void RegisterFile(string fullPath)
        {
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                Debug.Print("File does not exist: {0}", fullPath);
                return;
            }

            FileId id;

            FileAttributes attributes = File.GetAttributes(fullPath);
            bool isDir = (attributes & FileAttributes.Directory) == FileAttributes.Directory;

            try
            {
                if (isDir)
                {
                    Debug.Print("File is DIR: {0}", fullPath);
                    id = PushNewFolderToCloud(fullPath);
                }
                else
                {
                    id = PushNewFileToCloud(fullPath);
                }
            }
            catch (Exception exception)
            {
                Debug.Print(exception.ToString());
                Debug.Print("FAILED to sync {0}", fullPath);
                return;
            }

            try
            {
                ConvertToPlaceholder(fullPath, id, isDir);
                Debug.Print("File sync OK: {0}", fullPath);
            }
            catch (Exception exception)
            {
                Debug.Print(exception.ToString());
                Debug.Print("FAIL {0} : File was uploaded to cloud, however local file isn't linked with cloud", fullPath);
                return;
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
            metadata.FileSize = info.PropertiesSize;

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

        private void SetInSyncState(FileSystemEventArgs e)
        {
            SafeHCFFILE handle;
            HRESULT openHres = CfOpenFileWithOplock(e.FullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS, out handle);

            if (openHres != HRESULT.S_OK)
            {
                Debug.Print("Handle NOT OK");
            }
            HRESULT hresSync = CfSetInSyncState(handle.DangerousGetHandle(), CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
            if (hresSync != HRESULT.S_OK)
            {
                Debug.Print("SetSyncState NOT OK");
            }
            else
            {
                throw new Exception("CfSetInSyncState hres: " + hresSync);
            }

            CfCloseHandle(handle);
        }

        private void SetInSyncState(SafeHCFFILE handle)
        {
            HRESULT hresSync = CfSetInSyncState(handle.DangerousGetHandle(), CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
            if (hresSync != HRESULT.S_OK)
            {
                Debug.Print("SetSyncState NOT OK");
            }
            else
            {
                throw new Exception("CfSetInSyncState hres: " + hresSync);
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
                throw new Exception("CfOpenFileWithOplock: " + hresult.ToString());
            }

            nint fileIdentity = Marshal.StringToCoTaskMemUni(id.fileId);
            uint fileIdentityLength = (uint)id.fileId.Length * 2;

            unsafe
            {
                hresult = CfConvertToPlaceholder(protectedHandle.DangerousGetHandle(), fileIdentity, fileIdentityLength, CF_CONVERT_FLAGS.CF_CONVERT_FLAG_MARK_IN_SYNC);
            }
            if (hresult != HRESULT.S_OK)
            {
                CfCloseHandle(protectedHandle);
                Marshal.FreeCoTaskMem(fileIdentity);
                throw new Exception("CfConvertToPlaceholder: " + hresult);
            }
            CfCloseHandle(protectedHandle);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
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