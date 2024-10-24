using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Vanara.Extensions.Reflection;
using Vanara.PInvoke;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;
using static Vanara.PInvoke.CldApi;

public static class CloudProvider
{
    public static string ID = @"TestStorageProvider";
    public static string ACCOUNT = @"TestAccount";

    public static void RegisterWithShell(string folderPath)
    {
        StorageProviderSyncRootInfo info = new();
        info.DisplayNameResource = "Onedata";
        info.Id = GetSyncRootId();

        //info.Path = await StorageFolder.GetFolderFromPathAsync(folderPath);
        Task<StorageFolder> x = StorageFolder.GetFolderFromPathAsync(folderPath).AsTask();
        x.Wait();
        info.Path = x.Result;

        //string icon = "C:\\Users\\User\\Desktop\\win-client\\CloudSync\\bin\\Debug\\net8.0-windows10.0.26100.0" + "\\favicon.ico,0";
        string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        string icon = exeDir + "\\favicon.ico,0";

        Debug.Print("Icon path: {0}", icon);
        info.IconResource = icon;
        info.HydrationPolicy = StorageProviderHydrationPolicy.Full;
        info.HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.None;
        info.PopulationPolicy = StorageProviderPopulationPolicy.AlwaysFull;
        info.InSyncPolicy = StorageProviderInSyncPolicy.FileCreationTime | StorageProviderInSyncPolicy.DirectoryCreationTime;
        info.Version = "1.0.0";
        info.ShowSiblingsAsGroup = false;
        info.HardlinkPolicy = StorageProviderHardlinkPolicy.None;
        // all of those info parameters are required

        info.RecycleBinUri = new Uri("https://www.abcd.abcd.com/recyclebin");
        info.Context = CryptographicBuffer.ConvertStringToBinary(folderPath, BinaryStringEncoding.Utf8);

        StorageProviderSyncRootManager.Register(info);
        Debug.Print("SyncRoot ID: {0}", info.Id);

        var info2 = StorageProviderSyncRootManager.GetCurrentSyncRoots();
        Debug.Print("Number of syncRoots: " + info2.Count);
    }

    public static void UnregisterSafely(string syncRootId = "")
    {
        
        if (syncRootId == "")
        {
            syncRootId = GetSyncRootId();
        }

        var info = StorageProviderSyncRootManager.GetCurrentSyncRoots();
        int matchingSyncRootsCount = info.Where(x => x.Id == syncRootId).ToArray().Length;
        Debug.Print("Total number of syncRoots before unregister: " + info.Count);
        Debug.Print("Number of matching syncRoots before unregister: " + matchingSyncRootsCount);

        if (info.Where(x => x.Id == syncRootId).ToArray().Length < 1)
        {
            Debug.Print("Can not unregister syncRoot with id \"{0}\", because it does not exist", syncRootId);
            return;
        }

        StorageProviderSyncRootManager.Unregister(syncRootId);

        info = StorageProviderSyncRootManager.GetCurrentSyncRoots();
        matchingSyncRootsCount = info.Where(x => x.Id == syncRootId).ToArray().Length;
        Debug.Print("Total number of syncRoots after unregister: " + info.Count);
        Debug.Print("Number of matching syncRoots before unregister: " + matchingSyncRootsCount);

    }

    public static string GetSID()
    {
        SecurityIdentifier? currentSid = WindowsIdentity.GetCurrent().User;
        string sid = currentSid is null ? "empty" : currentSid.ToString();
        return sid;
    }

    public static string GetSyncRootId()
    {
        string syncRootId = ID + "!" + GetSID() + "!" + ACCOUNT;
        return syncRootId;
    }

    // passing local variables to C functions (eg. through Vanara) causes loss of reference in C#
    // and garbage collector might prematurely free memory which is still in use.
    // Solution is to use variables at higher level, so that you keep reference to it in C# code
    // eg. registrationTable is reference to CF_CALLBACK_REGISTRATION[]
    // Error caused by this: 'Process terminated. A callback was made on a garbage collected 
    // delegate of type 'Vanara.PInvoke.CldApi!Vanara.PInvoke.CldApi+CF_CALLBACK::Invoke'
    // this error happen on higher level and can not be caught using Catch()
    public static CF_CALLBACK_REGISTRATION[] registrationTable = [];

    public static CF_CALLBACK_REGISTRATION[] CreateCallbackTable()
    {
        registrationTable = new CF_CALLBACK_REGISTRATION[]
        {
            new CF_CALLBACK_REGISTRATION 
            {
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_DATA,
                Callback = new CF_CALLBACK(OnFetchData),
            },
            new CF_CALLBACK_REGISTRATION 
            {
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_DATA,
                Callback = new CF_CALLBACK(OnCancelFetchData),
            },
            new CF_CALLBACK_REGISTRATION 
            {
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DELETE,
                Callback = new CF_CALLBACK(OnDelete),
            },
            new CF_CALLBACK_REGISTRATION 
            {
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DELETE_COMPLETION,
                Callback = new CF_CALLBACK(OnDeleteCompletion),
            },
            new CF_CALLBACK_REGISTRATION 
            {
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_RENAME,
                Callback = new CF_CALLBACK(OnRename),
            },
            new CF_CALLBACK_REGISTRATION 
            {
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_RENAME_COMPLETION,
                Callback = new CF_CALLBACK(OnRenameCompletion),
            },
            new CF_CALLBACK_REGISTRATION 
            {
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_PLACEHOLDERS,
                Callback = new CF_CALLBACK(OnFetchPlaceholders),
            },
            CF_CALLBACK_REGISTRATION.CF_CALLBACK_REGISTRATION_END
        };
        return registrationTable;
    }

    public static CF_CONNECTION_KEY connectionKey;

    public static void ConnectCallbacks(string folderPath)
    {
        HRESULT hresConnectSyncRoot = CfConnectSyncRoot(
            folderPath,
            CreateCallbackTable(),
            IntPtr.Zero,
            CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_PROCESS_INFO | CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_FULL_FILE_PATH | CF_CONNECT_FLAGS.CF_CONNECT_FLAG_BLOCK_SELF_IMPLICIT_HYDRATION,
            out connectionKey
        );
        if (hresConnectSyncRoot != HRESULT.S_OK)
        {
            Debug.Print("HRES CfConnectSyncRoot: {0}", hresConnectSyncRoot);
            throw new Exception("Failed to connect callbacks");
        }
    }

    public static void DisconectCallbacks()
    {
        CfDisconnectSyncRoot(connectionKey);
    }

    public static void OnFetchPlaceholders(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.Print("FETCH PLACEHOLDERS");
        return;
    }

    public static void OnFetchData(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.Print("FETCH DATA");
        PrintInfo(CallbackInfo, CallbackParameters);
        
        CF_OPERATION_INFO oi = new()
            {
                Type = CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_DATA,
                ConnectionKey = CallbackInfo.ConnectionKey,
                TransferKey = CallbackInfo.TransferKey
            };
            oi.StructSize = (uint)Marshal.SizeOf(oi);
        
        IntPtr unmanagedPointer = IntPtr.Zero;
        CF_OPERATION_PARAMETERS.TRANSFERDATA td;
        CF_OPERATION_PARAMETERS op;

        try
        {
            string fileIdentity = Marshal.PtrToStringAuto(CallbackInfo.FileIdentity, (int) CallbackInfo.FileIdentityLength/2) ?? "";
            SpaceFolder space = CloudSync.spaces[Utils.GetSpaceName(CallbackInfo.VolumeDosName + CallbackInfo.NormalizedPath)];
            
            var taskInfo = RestClient.GetFileAttribute(fileIdentity, space.providerInfos);
            taskInfo.Wait();
            FileAttribute fileInfo = taskInfo.Result;
            if (fileInfo.size != CallbackInfo.FileSize)
            {
                throw new Exception("Size of cloud file does not match local file size. Try to refresh placeholders (R)");
            }

            Task<Stream> taskData = RestClient.GetStream(
                CloudSync.spaces[Utils.GetSpaceName(CallbackInfo.VolumeDosName + CallbackInfo.NormalizedPath)].providerInfos,
                Marshal.PtrToStringAuto(CallbackInfo.FileIdentity, (int) CallbackInfo.FileIdentityLength/2) ?? ""
                );
            taskData.Wait();

            Stream stream = taskData.Result;

            const int CHUNK = 4096 * 4;
            unmanagedPointer = Marshal.AllocHGlobal(CHUNK);
            byte[] buffer = new byte[CHUNK];
            int read;
            int offset = 0;

            td = new()
                {
                    CompletionStatus = new NTStatus((uint)CloudFilterEnum.STATUS_SUCCESS),
                    Buffer = unmanagedPointer,
                    Offset = 0,
                    Length = 0,
                    Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE
                };

            do
            {
                read = stream.Read(buffer, 0, CHUNK);
                

                while (read != CHUNK && read + offset < CallbackInfo.FileSize)
                {
                    read += stream.Read(buffer, read, CHUNK - read);
                }

                Marshal.Copy(buffer, 0, unmanagedPointer, read);

                td.Length = read;
                td.Offset = offset;

                offset += read;

                op = CF_OPERATION_PARAMETERS.Create(td);

                
                CfReportProviderProgress(CallbackInfo.ConnectionKey, CallbackInfo.TransferKey, CallbackInfo.FileSize, offset);
                

                var hres = CfExecute(oi,ref op);
                if (hres != HRESULT.S_OK) {
                    Debug.Print("Fetch data CfExecute FAIL: {0}", hres);
                    return;
                }
            } while(read == CHUNK);
            
        }
        catch (Exception e)
        {
            Debug.Print(e.Message);
            Debug.Print("FAILED TO FETCH DATA");
            td = new()
            {
                CompletionStatus = new NTStatus((uint)CloudFilterEnum.STATUS_CLOUD_FILE_REQUEST_ABORTED),
                Buffer = 0,
                Offset = 0,
                Length = 0,
                Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE
            };

            op = CF_OPERATION_PARAMETERS.Create(td);

            var hres = CfExecute(oi,ref op);
        }
        finally
        {
            Marshal.FreeHGlobal(unmanagedPointer);
            Debug.Print("");
        }
    }

    public static void OnCancelFetchData(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        // TODO (not needed)
        Debug.Print("OnCancelFetchData");
        return;
    }

    public static void OnDelete(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.Print("DELETE");
        PrintInfo(CallbackInfo, CallbackParameters);

        //nint file_identity = CallbackInfo.FileIdentity;
        
        CF_OPERATION_INFO oi = new()
        {
            Type = CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_DELETE,
            ConnectionKey = CallbackInfo.ConnectionKey,
            TransferKey = CallbackInfo.TransferKey
        };
        oi.StructSize = (uint)Marshal.SizeOf(oi);
        
        NTStatus status;

        try
        {
            if (CloudSync.configuration.root_path + Utils.GetSpaceName(CallbackInfo.VolumeDosName + CallbackInfo.NormalizedPath) ==
                CallbackInfo.VolumeDosName + CallbackInfo.NormalizedPath)
            {
                Debug.Print("Can not delete SPACE");
                throw new Exception("Can not delete space folder");
            }
            var task = RestClient.Delete(
                CloudSync.spaces[Utils.GetSpaceName(CallbackInfo.VolumeDosName + CallbackInfo.NormalizedPath)].providerInfos,
                Marshal.PtrToStringAuto(CallbackInfo.FileIdentity, (int) CallbackInfo.FileIdentityLength/2) ?? "");
            task.Wait();
            status = new NTStatus((uint)NTStatus.STATUS_SUCCESS);
        }
        catch (Exception)
        {
            status = new NTStatus((uint)CloudFilterEnum.STATUS_CLOUD_FILE_UNSUCCESSFUL);
        }

        CF_OPERATION_PARAMETERS.ACKDELETE del = new()
        {
            CompletionStatus = status,
            Flags = CF_OPERATION_ACK_DELETE_FLAGS.CF_OPERATION_ACK_DELETE_FLAG_NONE
        };
        CF_OPERATION_PARAMETERS op = CF_OPERATION_PARAMETERS.Create(del);

        var hres = CfExecute(oi,ref op);
        if (hres != HRESULT.S_OK) {
            Debug.Print("Delete CfExecute FAIL {0}", hres);
        }
        Debug.Print("");
        return;
    }

    public static void OnRename(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Thread.Sleep(100);

        // TODO: server api does not exist
        Debug.Print("RENAME");
        PrintInfo(CallbackInfo, CallbackParameters);

        string fileIdentity = Marshal.PtrToStringAuto(CallbackInfo.FileIdentity, (int) CallbackInfo.FileIdentityLength/2) ?? "";

        string targetPath = CallbackInfo.VolumeDosName + CallbackParameters.Rename.TargetPath;
        string sourcePath = CallbackInfo.VolumeDosName + CallbackInfo.NormalizedPath;
        string recycleBin = CallbackInfo.VolumeDosName + @"\$Recycle.Bin\";

        Debug.Print("Source path: {0}", sourcePath);
        Debug.Print("Target path: {0}", targetPath);
        
        CF_OPERATION_INFO oi = new()
        {
            Type = CF_OPERATION_TYPE.CF_OPERATION_TYPE_ACK_RENAME,
            ConnectionKey = CallbackInfo.ConnectionKey,
            TransferKey = CallbackInfo.TransferKey
        };
        oi.StructSize = (uint)Marshal.SizeOf(oi);
        
        NTStatus status = (uint) NTStatus.STATUS_SUCCESS;

        CloudSync.watcher.Pause();

        try
        {
            string spacePath = CloudSync.configuration.root_path + Utils.GetSpaceName(sourcePath) + "\\";

            if (targetPath.StartsWith(spacePath))
            {

                Debug.Print("Move/rename file within space");

                string src = sourcePath.Remove(0, spacePath.Length).Replace('\\','/');
                string dest = targetPath.Remove(0, spacePath.Length).Replace('\\','/');
                List<ProviderInfo> providerInfos = CloudSync.spaces[Utils.GetSpaceName(sourcePath)].providerInfos;
                var task = RestClient.Move(providerInfos, src, dest, Utils.GetSpaceName(sourcePath));
                task.Wait();

                Debug.Print("File was renamed/moved on cloud");

            }
            else if (targetPath.StartsWith(recycleBin))
            {
                // success
                Debug.Print("Move to recycle bin.");
            }
            else if (targetPath.StartsWith(CloudSync.configuration.root_path) && !targetPath.StartsWith(spacePath))
            {
                // fail
                Debug.Print("Can not move file to different space. Try to copy the file.");
                throw new Exception();
            }
            else
            {
                // success
                Debug.Print("Move file outside SyncRoot -> File deleted on cloud");
                List<ProviderInfo> providerInfos = CloudSync.spaces[Utils.GetSpaceName(sourcePath)].providerInfos;
                var taskRemove = RestClient.Delete(providerInfos, fileIdentity);
                taskRemove.Wait();
            }
        }
        catch (Exception e)
        {
            Debug.Print(e.Message);
            Debug.Print("Move/Rename FAIL");
            status = new NTStatus((uint)1);
        }
        

        CF_OPERATION_PARAMETERS.ACKRENAME rename = new()
        {
            CompletionStatus = status,
            Flags = CF_OPERATION_ACK_RENAME_FLAGS.CF_OPERATION_ACK_RENAME_FLAG_NONE
        };
        CF_OPERATION_PARAMETERS op = CF_OPERATION_PARAMETERS.Create(rename);

        var hres = CfExecute(oi,ref op);
        if (hres != HRESULT.S_OK) {
            Debug.Print("Rename CfExecute FAIL {0}", hres);
        }

        Thread.Sleep(250);
        CloudSync.watcher.Resume();

        Debug.Print("");
        return;
    }

    public static void OnRenameCompletion(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        string destPath = CallbackInfo.VolumeDosName + CallbackInfo.NormalizedPath;
        if (destPath.StartsWith(CloudSync.configuration.root_path))
        {
            HRESULT hresOpen = CfOpenFileWithOplock(destPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE, out SafeHCFFILE protectedHandle);
            HRESULT hresSync = CfSetInSyncState(protectedHandle.DangerousGetHandle(), CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
            CfCloseHandle(protectedHandle);
            if (hresSync == HRESULT.S_OK)
            {
                Debug.Print("Set InSync OK");
            }
            else 
            {
                Debug.Print("Open: {0}", hresOpen);
                Debug.Print("SetInSync: {0}", hresSync);
                Debug.Print("Set InSync FAIL");
            }
        }
    }

    public static void OnDeleteCompletion(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        // TODO
        Debug.Print("DELETE COMPLETITION");
        PrintInfo(CallbackInfo, CallbackParameters);
        // free memory (allocated during placeholder creation)
        // Marshal.FreeCoTaskMem(CallbackInfo.FileIdentity);

        Debug.Print("");
        return;
    }

    private static void PrintInfo(in CF_CALLBACK_INFO CallbackInfo, in CF_CALLBACK_PARAMETERS CallbackParameters)
    {
        Debug.Print("File identity {0}", Marshal.PtrToStringAuto(CallbackInfo.FileIdentity, (int) CallbackInfo.FileIdentityLength/2));
        Debug.Print("File path: {0}", CallbackInfo.NormalizedPath);
    }
}