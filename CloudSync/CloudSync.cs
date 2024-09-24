using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

class CloudSync
{
    public static Config configuration = new();
    public static Dictionary<string, SpaceFolder> spaces = new();
    public static FileWatcher watcher = new();
    public static int Run(string path = "", bool delete = false)
    {
        Console.WriteLine("CLOUD SYNC START");
        try
        {
            if (path.Length >= 1)
            {
                configuration = LoadConfig(path);
            }
            else 
            {
                configuration = LoadConfig();
            }
            Console.WriteLine("Load config -> OK");

            RestClient.Init(configuration);
            Console.WriteLine("Init Rest Client -> OK");

            InitSyncRootDir(delete);
            Console.WriteLine("SyncRoot directory -> OK: " + configuration.root_path);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e}");
            return 1;
        }

        try
        {
            CloudProvider.RegisterWithShell(configuration.root_path).Wait();
            Console.WriteLine("ShellRegister -> OK");

            CloudProvider.ConnectCallbacks(configuration.root_path);
            Console.WriteLine("ConnectCallbacks -> OK");

            InitSpaceFolders();

            // get placeholders withing space folders
            Console.WriteLine("CREATING PLACEHOLDERS WITHIN SPACES");
            foreach (SpaceFolder spaceFolder in spaces.Values)
            {   
                var task5 = RestClient.GetFilesAndSubdirs(spaceFolder.dirId, spaceFolder.providerInfos);
                task5.Wait();
                DirChildren children = task5.Result;

                ChildrenPlaceholders(children, spaceFolder.name, spaceFolder);
            }
            Console.WriteLine("Placeholders created");

            // start file watcher
            watcher = new(configuration.root_path);
            Console.WriteLine("FILEWATCHER START");

            // console waits for key hit -> so the program does not terminate
            Console.WriteLine("DONE: R -> refresh placeholders, ENTER -> terminate app");
            
            // handle user command
            ConsoleLogic();

            // end gracefully
            Console.WriteLine("Turning CloudSync Off");
            watcher.Dispose();
            Console.WriteLine("FileWatcher Stop");
        }
        catch (Exception e)
        {
            Console.WriteLine("CLOUD SYNC FAIL.");
            Console.WriteLine(e.ToString());
            return 1;
        }
        finally
        {
            CloudProvider.DisconectCallbacks();
            Console.WriteLine("Callbacks disconected");
            CloudProvider.UnregisterSafely();
            Console.WriteLine("SyncRoot unregistered");
        }
        RestClient.Dispose();
        Console.WriteLine("RestClient dispose");
        Console.WriteLine("TERMINATED NORMALLY (use \"Ctrl + C\" if needed)");
        return 0;
    }

    public static int Repair(string syncRootId = "")
    {
        CloudProvider.UnregisterSafely(syncRootId);
        return 0;
    }

    public static void InitSpaceFolders()
    {
        Console.WriteLine("CREATING SPACE FOLDERS");
        using (PlaceholderCreateInfo info = new())
        {
            var taskTA = RestClient.InferAccessTokenScope();
            taskTA.Wait();
            TokenAccess tokenAccess = taskTA.Result;

            foreach (KeyValuePair<string, TASpace> space in tokenAccess.dataAccessScope.spaces)
            // KEY is spaceId
            {
                string spaceName = space.Value.name;
                //SpaceDetails spaceDetails;
                SpaceFolder spaceFolder = new();

                bool placeholderAdded = false;

                // foreach provider supporting the space
                foreach (KeyValuePair<string, Support> support in space.Value.supports)
                // KEY is providerId
                {
                    string providerDomain = tokenAccess.dataAccessScope.providers[support.Key].domain;
                    string providerId = support.Key;
                    bool online = tokenAccess.dataAccessScope.providers[support.Key].online;

                    if (!online)
                    {
                        continue;
                    }

                    try
                    {
                        if (!placeholderAdded)
                        {
                            string dirId = space.Key;

                            var task5 = RestClient.GetFileAttribute(dirId, providerDomain);
                            task5.Wait();
                            FileAttribute fileInfo = task5.Result;

                            PlaceholderData placeholderData = new(
                                fileInfo.file_id, 
                                spaceName,
                                0,
                                fileInfo.atime,
                                fileInfo.mtime,
                                fileInfo.ctime);
                            info.Add(Placeholders.createDirInfo(placeholderData));
                            Console.WriteLine("Space: {0}", spaceName);

                            placeholderAdded = true;

                            spaceFolder = new(spaceName, fileInfo.file_id, space.Key, new ProviderInfo(providerId, providerDomain));
                        }
                        else
                        {
                            ProviderInfo providerInfo = new(providerId, providerDomain);
                            spaceFolder.providerInfos.Add(providerInfo);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Console.WriteLine("Trying another provider.");
                    }
                }
                if (placeholderAdded)
                {
                    spaces.Add(spaceFolder.name, spaceFolder);
                }
            }

            uint entries_processed;

            CF_PLACEHOLDER_CREATE_INFO[] infoArr = info.GetArray();

            HRESULT hres =  CfCreatePlaceholders(configuration.root_path, infoArr, (uint)infoArr.Length, CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out entries_processed);
            if (hres != HRESULT.S_OK)
            {
                Console.WriteLine();
                Console.WriteLine("FAILED to init space folders (CfCreatePlaceholders): ");
                Console.Write("     {0}", hres);
            }
            Console.WriteLine("Placeholder create OK -> (number of spaces) {0}", entries_processed);
        }
    }

    public static void ChildrenPlaceholders(DirChildren dirChildren, string rootDir, SpaceFolder spaceFolder)
    {
        List<SpaceFolder> childDirs = new();

        using (PlaceholderCreateInfo info = new())
        {
            foreach (Child child in dirChildren.children)
            {
                PlaceholderData data = new(child.file_id, child.name, child.size, child.atime, child.mtime, child.ctime);
                if (child.type == "DIR")
                {
                    info.Add(Placeholders.createDirInfo(data));
                    SpaceFolder dir = new()
                    {
                        dirId = child.file_id,
                        name = rootDir + @"\" + child.name
                    };
                    childDirs.Add(dir);
                }
                else if (child.type == "REG")
                {
                    info.Add(Placeholders.createInfo(data));
                }
            }

            uint entries_processed = 0;

            CF_PLACEHOLDER_CREATE_INFO[] infoArr = info.GetArray();

            if (infoArr.Length > 0)
            {
                HRESULT hres = CfCreatePlaceholders(configuration.root_path + rootDir + @"\", infoArr, (uint)infoArr.Length, CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out entries_processed);
                if (hres != HRESULT.S_OK)
                {
                    //Console.WriteLine();
                    Console.WriteLine("FAILED to init content of space folders (CfCreatePlaceholders): ");
                    Console.Write("     {0}", hres);
                }
            }
            Console.WriteLine("Placeholder create in dir:{0} -> {1} / {2}", configuration.root_path + rootDir + @"\", entries_processed, infoArr.Length);
            Console.WriteLine();
        }

        foreach (SpaceFolder dir in childDirs)
        {
            try
            {
                var task = RestClient.GetFilesAndSubdirs(dir.dirId, spaceFolder.providerInfos);
                task.Wait();
                DirChildren subChildren = task.Result;

                ChildrenPlaceholders(subChildren, dir.name, spaceFolder);
            }
            catch (Exception e)
            {   
                Console.WriteLine("Folder: {0}", dir.name);
                Console.WriteLine(e);
                Console.WriteLine();
            }
        }
    }

    public static Config LoadConfig(string configPath = "")
    {
        if (configPath == "")
        {
            configPath = Directory.GetCurrentDirectory() + @"\" + "config.json";
        }

        Console.WriteLine("Reading configuration from: {0}", configPath);

        Config config = new();
        config.Init(configPath);
        if (!config.IsComplete())
        {
            throw new Exception("Failed to load configuration from: {0}" + configPath + ". Fields must not be empty and root_path must end with \"\\\"");
        }
        return config;
    }

    public static void InitSyncRootDir(bool deleteExisting = false)
    {
        if (deleteExisting && Directory.Exists(configuration.root_path))
        {
            Directory.Delete(configuration.root_path, true);
        }

        if (!Directory.Exists(configuration.root_path))
        {
            _ = Directory.CreateDirectory(configuration.root_path);
            Console.WriteLine("Creating new SyncRoot Directory.");
        }
        
        // test root folder permissions
        File.Create(configuration.root_path + "testingAccess.txt").Close();
        File.Delete(configuration.root_path + "testingAccess.txt");

        if (Directory.EnumerateFileSystemEntries(configuration.root_path).Any())
        {
            throw new Exception("SyncRoot Directory must be empty.");
        }
    }

    public static void ConsoleLogic()
    {
        bool terminateApp = false;
        while (!terminateApp)
        {
            ConsoleKeyInfo key = Console.ReadKey();
            Console.WriteLine();
            if (key.Key == ConsoleKey.R)
            {
                Console.WriteLine("R was hit");
                RefreshPlaceholders();
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine("ENTER was hit -> terminate app");
                terminateApp = true;
            }
            else
            {
                Console.WriteLine("Unknown command: R -> refresh placeholders, ENTER -> terminate app");
            }
        }
    }

    public static void RefreshPlaceholders()
    {
        watcher.Pause();
        Console.WriteLine("REFRESHING PLACEHOLDERS");
        
        foreach (SpaceFolder space in spaces.Values)
        {
            try
            {
                Console.WriteLine("{0} - Refreshing placeholders", space.name);
                string folderPath = configuration.root_path + space.name;
                RefreshFolder(folderPath, space);
                Console.WriteLine("{0} - Placeholders refreshed", space.name);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Refresh FAILED: {0}", space.name);
            }
            
        }
        Console.WriteLine("Refresh placeholders FINISHED");

        watcher.Resume();
    }

    public static void RefreshFolder(string folderPath, SpaceFolder space)
    {
        if (!Directory.Exists(folderPath))
        {
            return;
        }
        List<string> folderPaths = new();
        using (PlaceholderCreateInfo createInfos = new())
        {
            FolderSetNotInSync(folderPath);
            // folder get file metadata
            CF_PLACEHOLDER_BASIC_INFO info = Utils.GetBasicInfo(folderPath);
            string id = System.Text.Encoding.Unicode.GetString(info.FileIdentity);

            DirChildren children;
            try
            {
                var task = RestClient.GetFilesAndSubdirs(id, space.providerInfos);
                task.Wait();
                children = task.Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Failed to refresh folder {0}", folderPath);
                Console.WriteLine();
                return;
            }
            

            // foreach metadata compare
            foreach (Child cloudFile in children.children)
            {
                string localPath = folderPath + "\\" + cloudFile.name;
                if (cloudFile.type == "DIR")
                {
                    if (Directory.Exists(localPath))
                    {
                        CF_PLACEHOLDER_BASIC_INFO localFolderInfo = Utils.GetBasicInfo(localPath);
                        string localFolderId = System.Text.Encoding.Unicode.GetString(localFolderInfo.FileIdentity);
                        if (localFolderId == cloudFile.file_id)
                        {
                            SetSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, localPath);
                        }
                        else
                        {
                            Directory.Delete(localPath, true);
                            createInfos.Add(CreatePlaceholderInfo(cloudFile));
                        }
                        
                    }
                    else
                    {
                        createInfos.Add(CreatePlaceholderInfo(cloudFile));
                    }
                    folderPaths.Add(localPath);
                }
                else if (cloudFile.type == "REG")
                {
                    if (File.Exists(localPath))
                    {
                        CF_PLACEHOLDER_STANDARD_INFO localFileInfo = Utils.GetStandardInfo(localPath);
                        string localFolderId = System.Text.Encoding.Unicode.GetString(localFileInfo.FileIdentity);
                        FileInfo localFileMetadata = new(localPath);

                        // compare id, size, mtime
                        if (localFolderId == cloudFile.file_id 
                            && localFileMetadata.Length == cloudFile.size 
                            && ((DateTimeOffset)localFileMetadata.LastWriteTime).ToUnixTimeSeconds() == cloudFile.mtime)
                        {
                            SetSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, localPath);
                        }
                        else
                        {
                            File.Delete(localPath);
                            createInfos.Add(CreatePlaceholderInfo(cloudFile));
                        }
                    }
                    else
                    {
                        createInfos.Add(CreatePlaceholderInfo(cloudFile));
                    }
                }
            }

            // create new placeholders
            uint entries_processed;

            CF_PLACEHOLDER_CREATE_INFO[] infoArr = createInfos.GetArray();

            if (infoArr.Length > 0)
            {
                HRESULT hres =  CfCreatePlaceholders(folderPath, infoArr, (uint)infoArr.Length, CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out entries_processed);
                if (hres != HRESULT.S_OK)
                {
                    Console.Write("FAILED to create new placeholders (CfCreatePlaceholders): {0}", hres);
                }
                Console.WriteLine("Placeholder create in dir:{0} -> {1} / {2}", folderPath, entries_processed, infoArr.Length);
                Console.WriteLine();
            }
        }
        // check files -> if unsync -> delete
        RemoveOutOfSyncFileFolder(folderPath);

        // enter subfolders -> repeat
        foreach (string folder in folderPaths)
        {
            RefreshFolder(folder,space);
        }
    }

    public static void RemoveOutOfSyncFileFolder(string folderPath)
    {
        foreach (string file in Directory.GetFiles(folderPath))
        {
            CF_PLACEHOLDER_BASIC_INFO info = Utils.GetBasicInfo(file);
            if (info.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("Failed to Delete {0}", file);
                }
            }
        }
        foreach (string dir in Directory.GetDirectories(folderPath))
        {
            CF_PLACEHOLDER_BASIC_INFO info = Utils.GetBasicInfo(dir);
            if (info.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC)
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("Failed to Delete {0}", dir);
                }
            }
        }
    }

    public static CF_PLACEHOLDER_CREATE_INFO CreatePlaceholderInfo(Child child)
    {
        CF_PLACEHOLDER_CREATE_INFO info;
        PlaceholderData data = new(
            child.file_id,
            child.name,
            child.size,
            child.atime,
            child.mtime,
            child.ctime);
        if (child.type == "DIR")
        {
            info = Placeholders.createDirInfo(data);
        }
        else 
        {
            info = Placeholders.createInfo(data);
        }
        return info;
    }

    public static void FolderSetNotInSync(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
        foreach (string file in Directory.GetFiles(path))
        {
            SetSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC, file);
        }
        foreach (string folder in Directory.GetDirectories(path))
        {
            SetSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC, folder);
        }
    }

    public static void SetSyncState(CF_IN_SYNC_STATE state, string path)
    {
        SafeHCFFILE handle;
        HRESULT hresOpen = CfOpenFileWithOplock(path, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS, out handle);
        if (hresOpen != HRESULT.S_OK)
        {
            throw new Exception($"SetSyncState CfOpenFileWithOplock {path} : " + hresOpen);
        }
        HRESULT hresSync = CfSetInSyncState(handle.DangerousGetHandle(), state, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
        CfCloseHandle(handle);

        if (hresSync != HRESULT.S_OK)
        {
            throw new Exception($"SetSyncState CfSetSyncState {path} :" + hresOpen);
        }
    }
}