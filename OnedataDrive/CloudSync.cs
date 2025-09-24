using NLog;
using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.SearchApi;

namespace OnedataDrive
{
    public static class CloudSync
    {
        public static Config configuration = new();
        public static Dictionary<string, SpaceFolder> spaces = new(); // spaces: KEY is space name
        public static FileWatcher watcher = new();
        public static bool running { get; private set; } = false;
        public static Logger logger = LogManager.GetCurrentClassLogger();
        public const string VERSION = "0.5.4";
        public const string APP_NAME = "Onedata Drive";

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

            CloudSyncReturnCodes status = InitSyncRootDir(delete);
            if (status != CloudSyncReturnCodes.SUCCESS)
            {
                return status;
            }
            logger.Info("SyncRoot directory -> OK: " + configuration.root_path);
            

            try
            {
                RestClient.Init(configuration);
                logger.Info("Init Rest Client -> OK");

                TestTokenAndOnezone();
                logger.Info("Test Token and Onezone -> OK");

                AddFolderToSearchIndexer(configuration.root_path);
                logger.Info("Add Folder To Search Indexer -> OK");

                CloudProvider.RegisterWithShell(configuration.root_path);
                logger.Info("ShellRegister -> OK");

                InitSpaceFolders();

                CloudProvider.ConnectCallbacks(configuration.root_path);
                logger.Info("ConnectCallbacks -> OK");

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
            foreach (var space in spaces.Values)
            {
                space.autoRefresh?.StopMonitoring();
            }
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
                                info.Add(Placeholders.CreateDirInfo(placeholderData));

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

        public static void CreatePlaceholders(PlaceholderCreateInfo info, string path)
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

        public static CloudSyncReturnCodes InitSyncRootDir(bool deleteExisting = false)
        {
            try
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
                return CloudSyncReturnCodes.SUCCESS;
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
        }
    }
}