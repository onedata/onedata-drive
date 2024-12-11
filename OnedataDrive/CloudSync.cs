using System.Diagnostics;
using Vanara.PInvoke;
using OnedataDrive.CloudSync.Exceptions;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.SearchApi;
using OnedataDrive.CloudSync.ErrorHandling;
using OnedataDrive.CloudSync.Utils;
using OnedataDrive.CloudSync.JSON_Object;

public static class CloudSync
{
    public static Config configuration = new();
    public static Dictionary<string, SpaceFolder> spaces = new();
    public static FileWatcher watcher = new();
    /// <summary>
    /// Method to start CloudSync
    /// </summary>
    /// <param name="config">Configuration of CloudSync</param>
    /// <param name="delete">If true, already existing root directory and its contents will be deleted</param>
    /// <returns></returns>
    public static CloudSyncReturnCodes Run(Config config, bool delete = false)
    {
        Debug.Print("CLOUD SYNC START");
        configuration = config;
        spaces = new();
        try
        {
            InitSyncRootDir(delete);
            Debug.Print("SyncRoot directory -> OK: " + configuration.root_path);
        }
        catch (RootFolderNotEmptyException)
        {
            return CloudSyncReturnCodes.ROOT_FOLDER_NOT_EMPTY;
        }
        catch (UnauthorizedAccessException)
        {
            return CloudSyncReturnCodes.ROOT_FOLDER_NO_ACCESS_RIGHT;
        }
        catch (IOException)
        {
            return CloudSyncReturnCodes.ROOT_FOLDER_NO_ACCESS_RIGHT;
        }
        catch (Exception e)
        {
            Debug.Print($"Error: {e}");
            return CloudSyncReturnCodes.ERROR;
        }

        try
        {
            RestClient.Init(configuration);
            Debug.Print("Init Rest Client -> OK");

            AddFolderToSearchIndexer(configuration.root_path);
            Debug.Print("Add Folder To Search Indexer -> OK");

            CloudProvider.RegisterWithShell(configuration.root_path);
            Debug.Print("ShellRegister -> OK");

            CloudProvider.ConnectCallbacks(configuration.root_path);
            Debug.Print("ConnectCallbacks -> OK");

            TestTokenValidity();

            InitSpaceFolders();

            InitSpaceFoldersChildren();

            // start file watcher
            watcher = new(configuration.root_path);
            Debug.Print("Filewatcher Start -> OK");
        }
        catch (OnezoneException e)
        {
            Stop();
            Debug.Print("CLOUD SYNC FAIL -> Onezone");
            Debug.Print(e.ToString());
            return CloudSyncReturnCodes.ONEZONE_FAIL;
        }
        catch (ProviderTokenException e)
        {
            Stop();
            Debug.Print("CLOUD SYNC FAIL -> Provider Token");
            Debug.Print(e.ToString());
            return CloudSyncReturnCodes.TOKEN_FAIL;
        }
        catch (Exception e)
        {
            Stop();
            Debug.Print("CLOUD SYNC FAIL.");
            Debug.Print(e.ToString());
            return CloudSyncReturnCodes.ERROR;
        }
        return CloudSyncReturnCodes.SUCCESS;
    }

    public static void Stop()
    {
        CloudProvider.DisconectCallbacks();
        Debug.Print("Callbacks disconected");
        CloudProvider.UnregisterSafely();
        Debug.Print("SyncRoot unregistered");
        RestClient.Stop();
        watcher.Dispose();
    }

    public static void AddFolderToSearchIndexer(string rootPath)
    {
        string url = @"file:" + rootPath;

        ISearchManager searchManager = (ISearchManager) new CSearchManager();
        ISearchCatalogManager searchCatalogManager = searchManager.GetCatalog("SystemIndex");
        ISearchCrawlScopeManager searchCrawlScopeManager = searchCatalogManager.GetCrawlScopeManager();
        searchCrawlScopeManager.AddDefaultScopeRule(url, true, FOLLOW_FLAGS.FF_INDEXCOMPLEXURLS);
        searchCrawlScopeManager.SaveAll();

        Debug.Print("AddFolderToSearchIndexer with path: " + url);
    }

    public static int Repair(string syncRootId = "")
    {
        CloudProvider.UnregisterSafely(syncRootId);
        return 0;
    }

    private static void TestTokenValidity()
    {
        var task = RestClient.ExamineToken();
        task.Wait();
        TokenExamine te = task.Result;
        if (!te.isRestInterface())
        {
            throw new ProviderTokenException("Wrong token interface");
        }
    }

    public static TokenAccess InferTokenAccess()
    {
        try
        {
            var taskTA = RestClient.InferAccessTokenScope();
            taskTA.Wait();
            return taskTA.Result;
        }
        catch (AggregateException e)
        {
            HttpRequestException? hre = e.InnerException as HttpRequestException;
            if (hre is not null && hre.Message.Contains("No such host is known."))
            {
                throw new OnezoneException("", hre);
            }
            else if (hre is not null && hre.Message.Contains(
                "Response status code does not indicate success: 400 (Bad Request)."))
            {
                throw new ProviderTokenException("", hre);
            }
            else
            {
                throw;
            }
        }
    }

    public static void InitSpaceFolders()
    {
        Debug.Print("CREATING SPACE FOLDERS");
        using (PlaceholderCreateInfo info = new())
        {
            TokenAccess tokenAccess = InferTokenAccess();

            foreach (KeyValuePair<string, TASpace> space in tokenAccess.dataAccessScope.spaces)
            // KEY is spaceId
            {
                string spaceName = space.Value.name;
                SpaceFolder spaceFolder = new();

                bool placeholderAdded = false;

                // foreach provider supporting the space
                // KEY is providerId
                foreach (KeyValuePair<string, Support> support in space.Value.supports)
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
                            Debug.Print("Space: {0}", spaceName);

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
                        Debug.Print(e.Message);
                        Debug.Print("Trying another provider.");
                    }
                }
                if (placeholderAdded)
                {
                    spaces.Add(spaceFolder.name, spaceFolder);
                }
            }

            CreatePlaceholders(info, configuration.root_path);
            
        }
        Debug.Print("CREATING SPACE FOLDERS - FINISHED");
    }

    private static void CreatePlaceholders(PlaceholderCreateInfo info, string path)
    {
        uint entriesProcessed = 0;

        CF_PLACEHOLDER_CREATE_INFO[] infoArr = info.GetArray();

        if (infoArr.Length > 0)
        {
            HRESULT hres = CfCreatePlaceholders(path, infoArr, (uint)infoArr.Length, CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out entriesProcessed);
            if (hres != HRESULT.S_OK)
            {
                Debug.Print("FAILED to init placeholders (CfCreatePlaceholders). HRESULT: {0}", hres);
            }
        }
        //Debug.Print("Placeholder create OK -> number of created: {0}", entriesProcessed);
        Debug.Print("Placeholders created in dir:{0} -> {1} / {2}", path, entriesProcessed, infoArr.Length);
    }

    public static void InitSpaceFoldersChildren()
    {
        Debug.Print("CREATING PLACEHOLDERS WITHIN SPACES");
        foreach (SpaceFolder spaceFolder in spaces.Values)
        {
            var task5 = RestClient.GetFilesAndSubdirs(spaceFolder.dirId, spaceFolder.providerInfos);
            task5.Wait();
            DirChildren children = task5.Result;

            ChildrenPlaceholders(children, spaceFolder.name, spaceFolder);
        }
        Debug.Print("CREATING PLACEHOLDERS WITHIN SPACES - FINISHED");
    }

    public static void ChildrenPlaceholders(DirChildren dirChildren, string rootDir, SpaceFolder spaceFolder)
    {
        NameConvertor nameConvertor = new NameConvertor();
        List<SpaceFolder> childDirs = new();

        using (PlaceholderCreateInfo info = new())
        {
            foreach (Child child in dirChildren.children)
            {
                string windowsCorrectName = DistinctWindowsName(child.name, info);

                PlaceholderData data = new(child.file_id, windowsCorrectName, child.size, child.atime, child.mtime, child.ctime);
                if (child.type == "DIR")
                {
                    info.Add(Placeholders.createDirInfo(data));
                    SpaceFolder dir = new()
                    {
                        dirId = child.file_id,
                        name = rootDir + @"\" + windowsCorrectName
                    };
                    childDirs.Add(dir);
                }
                else if (child.type == "REG")
                {
                    info.Add(Placeholders.createInfo(data));
                }
            }

            string path = configuration.root_path + rootDir + @"\";
            CreatePlaceholders(info, path);
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
                Debug.Print("Folder: {0}", dir.name);
                Debug.Print(e.Message);
                Debug.Print("");
            }
        }
    }

    private static string DistinctWindowsName(string name, PlaceholderCreateInfo info)
    {
        NameConvertor nameConvertor = new NameConvertor();
        string windowsCorrectName = nameConvertor.MakeWindowsCorrect(name);
        string suffix = "";
        for (int i = 2; info.Get().Any(x => x.RelativeFileName == (windowsCorrectName + suffix)); i++)
        {
            suffix = "(" + i.ToString() + ")";
        }
        return windowsCorrectName + suffix;
    }

    public static Config LoadConfig(string configPath = "")
    {
        if (configPath == "")
        {
            configPath = Directory.GetCurrentDirectory() + @"\" + "config.json";
        }

        Debug.Print("Reading configuration from: {0}", configPath);

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
            Debug.Print("Creating new SyncRoot Directory.");
        }
        
        // test root folder permissions
        File.Create(configuration.root_path + "testingAccess.txt").Close();
        File.Delete(configuration.root_path + "testingAccess.txt");

        if (Directory.EnumerateFileSystemEntries(configuration.root_path).Any())
        {
            throw new RootFolderNotEmptyException("SyncRoot Directory must be empty.");
        }
    }

    public static void ConsoleLogic()
    {
        bool terminateApp = false;
        while (!terminateApp)
        {
            ConsoleKeyInfo key = Console.ReadKey();
            Debug.Print("");
            if (key.Key == ConsoleKey.R)
            {
                Debug.Print("R was hit");
                RefreshPlaceholders();
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                Debug.Print("ENTER was hit -> terminate app");
                terminateApp = true;
            }
            else
            {
                Debug.Print("Unknown command: R -> refresh placeholders, ENTER -> terminate app");
            }
        }
    }

    public static void RefreshPlaceholders()
    {
        watcher.Pause();
        Debug.Print("REFRESHING PLACEHOLDERS");
        
        foreach (SpaceFolder space in spaces.Values)
        {
            try
            {
                Debug.Print("{0} - Refreshing placeholders", space.name);
                string folderPath = configuration.root_path + space.name;
                RefreshFolder(folderPath, space);
                Debug.Print("{0} - Placeholders refreshed", space.name);
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                Debug.Print("Refresh FAILED: {0}", space.name);
            }
            
        }
        Debug.Print("Refresh placeholders FINISHED");

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
            CF_PLACEHOLDER_BASIC_INFO info = CldApiUtils.GetBasicInfo(folderPath);
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
                Debug.Print(e.Message);
                Debug.Print("Failed to refresh folder {0}", folderPath);
                Debug.Print("");
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
                        CF_PLACEHOLDER_BASIC_INFO localFolderInfo = CldApiUtils.GetBasicInfo(localPath);
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
                        CF_PLACEHOLDER_STANDARD_INFO localFileInfo = CldApiUtils.GetStandardInfo(localPath);
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
                Debug.Print("Placeholder create in dir:{0} -> {1} / {2}", folderPath, entries_processed, infoArr.Length);
                Debug.Print("");
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
            CF_PLACEHOLDER_BASIC_INFO info = CldApiUtils.GetBasicInfo(file);
            if (info.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Debug.Print(e.Message);
                    Debug.Print("Failed to Delete {0}", file);
                }
            }
        }
        foreach (string dir in Directory.GetDirectories(folderPath))
        {
            CF_PLACEHOLDER_BASIC_INFO info = CldApiUtils.GetBasicInfo(dir);
            if (info.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC)
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (Exception e)
                {
                    Debug.Print(e.Message);
                    Debug.Print("Failed to Delete {0}", dir);
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