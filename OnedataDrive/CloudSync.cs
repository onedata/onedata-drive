using System.Diagnostics;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.SearchApi;
using OnedataDrive.Utils;
using OnedataDrive.JSON_Object;
using OnedataDrive.ErrorHandling;
using NLog;
using NLog.Targets;
using System.Runtime.InteropServices;

namespace OnedataDrive
{
    public static class CloudSync
    {
        public static Config configuration = new();
        public static Dictionary<string, SpaceFolder> spaces = new();
        public static FileWatcher watcher = new();
        public static bool running { get; private set; } = false;
        public static Logger logger = LogManager.GetCurrentClassLogger();
        public const string VERSION = "0.2.1";
        /// <summary>
        /// Method to start CloudSync
        /// </summary>
        /// <param name="config">Configuration of CloudSync</param>
        /// <param name="delete">If true, already existing root directory and its contents will be deleted</param>
        /// <returns></returns>
        public static CloudSyncReturnCodes Run(Config config, bool delete = false)
        {
            logger.Info("CLOUD SYNC: Start Connecting");

            configuration = config;
            spaces = new();
            try
            {
                InitSyncRootDir(delete);
                logger.Info("SyncRoot directory -> OK: " + configuration.root_path);
            }
            catch (Exception e)
            {
                logger.Error($"Failed to create Root Folder, {e}");

                if (e is RootFolderNotEmptyException)
                {
                    return CloudSyncReturnCodes.ROOT_FOLDER_NOT_EMPTY;
                }
                if (e is UnauthorizedAccessException || e is IOException)
                {
                    return CloudSyncReturnCodes.ROOT_FOLDER_NO_ACCESS_RIGHT;
                }

                return CloudSyncReturnCodes.ERROR;
            }

            try
            {
                RestClient.Init(configuration);
                logger.Info("Init Rest Client -> OK");

                AddFolderToSearchIndexer(configuration.root_path);
                logger.Info("Add Folder To Search Indexer -> OK");

                CloudProvider.RegisterWithShell(configuration.root_path);
                logger.Info("ShellRegister -> OK");

                CloudProvider.ConnectCallbacks(configuration.root_path);
                logger.Info("ConnectCallbacks -> OK");

                TestTokenAndOnezone();
                InitSpaceFolders();

                InitSpaceFoldersChildren();

                // start file watcher
                watcher = new(configuration.root_path);
                logger.Info("Filewatcher Start -> OK");
            }
            catch (OnezoneException e)
            {
                Stop();
                logger.Error($"CLOUD SYNC FAIL -> Onezone, {e}");
                return CloudSyncReturnCodes.ONEZONE_FAIL;
            }
            catch (ProviderTokenException e)
            {
                Stop();
                logger.Error($"CLOUD SYNC FAIL -> Provider Token, {e}");

                if (e is InvalidTokenType)
                {
                    return CloudSyncReturnCodes.INVALID_TOKEN_TYPE;
                }
                return CloudSyncReturnCodes.TOKEN_FAIL;
            }
            catch (Exception e)
            {
                Stop();
                logger.Error($"CLOUD SYNC FAIL, {e}");
                return CloudSyncReturnCodes.ERROR;
            }
            running = true;
            logger.Info("CLOUD SYNC IS RUNNING");
            return CloudSyncReturnCodes.SUCCESS;
        }

        public static void Stop()
        {
            watcher.Pause();
            CloudProvider.DisconectCallbacks();
            logger.Info("Callbacks disconected");
            CloudProvider.UnregisterSafely();
            logger.Info("SyncRoot unregistered");
            RestClient.Stop();
            logger.Info("Rest client stopped");
            watcher.Dispose();
            logger.Info("FileWatcher stopped");
            running = false;
            logger.Info("CLOUD SYNC STOPPED");
        }

        /// <summary>
        /// Add mounted folder to Windows Search Indexing service
        /// </summary>
        public static void AddFolderToSearchIndexer(string rootPath)
        {
            try
            {
                ISearchManager searchManager = (ISearchManager)new CSearchManager();
                ISearchCatalogManager searchCatalogManager = searchManager.GetCatalog("SystemIndex");
                ISearchCrawlScopeManager searchCrawlScopeManager = searchCatalogManager.GetCrawlScopeManager();

                string url = @"file:" + rootPath;
                searchCrawlScopeManager.AddDefaultScopeRule(url, true, FOLLOW_FLAGS.FF_INDEXCOMPLEXURLS);
                searchCrawlScopeManager.SaveAll();

                logger.Info("AddFolderToSearchIndexer with path: " + url);
            }
            catch (COMException e)
            {
                logger.Warn($"Failed to add folder to search indexer, {e}");
            }
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
                throw new InvalidTokenType("Wrong token interface");
            }
        }

        private static void TestTokenAndOnezone()
        {
            InferTokenAccess();
            TestTokenValidity();
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
            logger.Info("CREATING SPACE FOLDERS");
            using (PlaceholderCreateInfo info = new())
            {
                TokenAccess tokenAccess = InferTokenAccess();
                logger.Info("Available spaces: " 
                    + String.Join(" | " ,tokenAccess.dataAccessScope.spaces.Values.Select(o => o.name)));

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
                            logger.Warn($"Registering Space with provider FAILED: {providerDomain}, space {spaceFolder.name}, {e}");
                        }
                    }
                    if (placeholderAdded)
                    {
                        spaces.Add(spaceFolder.name, spaceFolder);
                        logger.Info("Space Registered: {0}", spaceName);
                    }
                    else
                    {
                        logger.Warn("Space is NOT supported by Oneprovider: {0}", spaceName);
                    }
                }

                CreatePlaceholders(info, configuration.root_path);

            }
            logger.Info("CREATING SPACE FOLDERS - FINISHED");
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
                    logger.Error("FAILED to init placeholders (CfCreatePlaceholders). HRESULT: {0}", hres);
                }
            }
            logger.Debug("Placeholders created in dirPath:{0} -> {1} / {2}", path, entriesProcessed, infoArr.Length);
        }

        public static void InitSpaceFoldersChildren()
        {
            logger.Info("CREATING PLACEHOLDERS WITHIN SPACES");
            foreach (SpaceFolder spaceFolder in spaces.Values)
            {
                var task5 = RestClient.GetFilesAndSubdirs(spaceFolder.dirId, spaceFolder.providerInfos);
                task5.Wait();
                DirChildren children = task5.Result;

                ChildrenPlaceholders(children, spaceFolder.name, spaceFolder);
            }
            logger.Info("CREATING PLACEHOLDERS WITHIN SPACES - FINISHED");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dirChildren"></param>
        /// <param name="rootDir">path starting from space</param>
        /// <param name="spaceFolder"></param>
        public static void ChildrenPlaceholders(DirChildren dirChildren, string rootDir, SpaceFolder spaceFolder)
        {
            NameConvertor nameConvertor = new NameConvertor();
            // childDirs - name parameter conains full path from space 
            List<SpaceFolder> childDirs = new();

            using (PlaceholderCreateInfo info = new())
            {
                foreach (Child child in dirChildren.children)
                {
                    string windowsCorrectName = DistinctWindowsName(child, info);

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

            foreach (SpaceFolder dirPath in childDirs)
            {
                try
                {
                    var task = RestClient.GetFilesAndSubdirs(dirPath.dirId, spaceFolder.providerInfos);
                    task.Wait();
                    DirChildren subChildren = task.Result;

                    ChildrenPlaceholders(subChildren, dirPath.name, spaceFolder);
                }
                catch (Exception e)
                {
                    logger.Error($"Space children - folder: {dirPath.name}, {e}");
                }
            }
        }

        private static string DistinctWindowsName(Child child, PlaceholderCreateInfo info)
        {
            NameConvertor nameConvertor = new NameConvertor();
            string windowsCorrectName;
            windowsCorrectName = nameConvertor.MakeWindowsCorrectDistinct(child.name, child.file_id, info);
            return windowsCorrectName;
        }

        public static Config LoadConfig(string configPath = "")
        {
            if (configPath == "")
            {
                configPath = Directory.GetCurrentDirectory() + @"\" + "config.json";
            }

            logger.Info("Reading configuration from: {0}", configPath);

            Config config = new();
            config.Init(configPath);
            if (!config.IsComplete())
            {
                throw new ConfigurationException("Failed to load configuration from: {0}" + configPath + ". Fields must not be empty.");
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
                logger.Info("Creating new SyncRoot Directory.");
            }

            // test root folder permissions
            File.Create(configuration.root_path + "testingAccess.txt").Close();
            File.Delete(configuration.root_path + "testingAccess.txt");

            if (Directory.EnumerateFileSystemEntries(configuration.root_path).Any())
            {
                throw new RootFolderNotEmptyException("SyncRoot Directory must be empty.");
            }
        }
    }
}