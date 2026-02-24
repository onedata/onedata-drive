using NLog;
using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.SearchApi;

namespace OnedataDrive
{
    public static class CloudSync
    {
        public const string VERSION = "0.5.7";
        public const string APP_NAME = "Onedata Drive";
        public static Logger logger = LogManager.GetCurrentClassLogger();
        public static Config configuration = new();
        public static Dictionary<string, SpaceFolder> spaces = new(); // spaces: KEY is space name
        public static FileWatcher? watcher = default;
        public static bool running { get; private set; } = false;
        public static RunningTaksList runningTasks = new(10);

        private static CancellationTokenSource cts = new();
        private static Task startupTask = Task.CompletedTask;


        /// <summary>
        /// Method to start CloudSync
        /// </summary>
        /// <param name="config">Configuration of CloudSync</param>
        /// <param name="delete">If true, already existing root directory and its contents will be deleted</param>
        /// <returns></returns>
        public static async Task<CloudSyncReturnCodes> RunAsync(Config config)
        {
            cts = new();
            logger.Info("CLOUD SYNC: Start Connecting");

            configuration = config;
            logger.Info($"CLOUD SYNC: Autorefresh - {config.enableRefresh}");
            logger.Info($"CLOUD SYNC: DeleteExistingRootDir - {config.deleteExistingRootDir}");

            spaces = new();

            List<Step> startupSteps = CreateStartupSteps();
            PipelineRunner runner = new(logger);
            runner.AddSteps(startupSteps);
            try
            {
                startupTask = runner.RunAsync(cts.Token);
                await startupTask;
            }
            catch (OperationCanceledException e)
            {
                logger.Error($"CLOUD SYNC FAIL -> Startup CANCELED, {e}");
                return CloudSyncReturnCodes.STARTUP_CANCELED;
            }
            catch (RootFolderNotEmptyException e)
            {
                logger.Error($"CLOUD SYNC FAIL -> Root Folder Not Empty, {e}");
                return CloudSyncReturnCodes.ROOT_FOLDER_NOT_EMPTY;
            }
            catch (RootFolderAcessException e)
            {
                logger.Error($"CLOUD SYNC FAIL -> Root Folder - No Access Right, {e}");
                return CloudSyncReturnCodes.ROOT_FOLDER_NO_ACCESS_RIGHT;
            }
            catch (OnezoneException e)
            {
                logger.Error($"CLOUD SYNC FAIL -> Invalid Onezone, {e}");
                return CloudSyncReturnCodes.ONEZONE_FAIL;
            }
            catch (ProviderTokenException e)
            {
                logger.Error($"CLOUD SYNC FAIL -> Provider Token, {e}");
                if (e is InvalidTokenType)
                {
                    return CloudSyncReturnCodes.INVALID_TOKEN_TYPE;
                }
                return CloudSyncReturnCodes.TOKEN_FAIL;
            }
            catch (Exception e)
            {
                logger.Error($"CLOUD SYNC FAIL -> Startup, {e}");
                return CloudSyncReturnCodes.ERROR;
            }
            
            running = true;
            logger.Info("CLOUD SYNC IS RUNNING");

            return CloudSyncReturnCodes.SUCCESS;
        }

        public static async Task Stop()
        {
            if (running)
            {
                TurnOff();
            }
            else
            {
                cts.Cancel();
                try
                {
                    await startupTask;
                }
                catch (Exception)
                {
                    // ignore
                }
                logger.Info("Startup task canceled");
                if (running)
                {
                    TurnOff();
                }
            }
            running = false;
            logger.Info("CLOUD SYNC STOPPED");
        }

        private static void TurnOff()
        {
            if (running)
            {
                watcher?.Pause();
                CloudProvider.DisconectCallbacks();
                logger.Info("Callbacks disconected");
                CloudProvider.UnregisterSafely();
                logger.Info("SyncRoot unregistered");
                RestClient.Stop();
                logger.Info("Rest client stopped");
                watcher?.Dispose();
                logger.Info("FileWatcher stopped");
                foreach (var space in spaces.Values)
                {
                    space.autoRefresh?.StopMonitoring();
                }
                runningTasks.Dispose();
            }
        }

        public static List<Step> CreateStartupSteps()
        {
            List<Step> steps = new();

            steps.Add(new Step 
            { 
                Name = "InitRunningTaskList",
                Run = (token) => Task.Run(() => {
                    runningTasks.Initialize();
                    logger.Info("RunningTaskList OK");
                }),
                Undo = () => Task.Run(() => runningTasks.Dispose())
            });

            steps.Add(new Step
            {
                Name = "InitRootDir",
                Run = (token) => Task.Run(() =>
                {
                    InitSyncRootDir(token);
                    logger.Info("SyncRootDir OK");
                }),
                Undo = () => Task.CompletedTask
            });
            steps.Add(new Step
            {
                Name = "AddFolderToSearchIndexer",
                Run = (token) => Task.Run(() => {
                    AddFolderToSearchIndexer(configuration.root_path);
                    logger.Info("AddFolderToSearchIndexer OK");
                }),
                Undo = () => Task.CompletedTask
            });

            steps.Add(new Step
            {
                Name = "InitRestClient",
                Run = (token) => Task.Run(() => { 
                    RestClient.Init(configuration);
                    logger.Info("RestInit OK"); 
                }),
                Undo = () => Task.Run(() => RestClient.Stop())
            });

            steps.Add(new Step
            {
                Name = "TestTokenAndOnezone",
                Run = (token) => Task.Run(() => { 
                    TestTokenAndOnezone(token); 
                    logger.Info("TestTokenAndOnezone OK"); 
                }),
                Undo = () => Task.CompletedTask
            });

            steps.Add(new Step
            {
                Name = "ShellRegister",
                Run = (token) => Task.Run(() => {
                    CloudProvider.RegisterWithShell(configuration.root_path);
                    logger.Info("RegisterWithShell OK");
                }),
                Undo = () => Task.Run(() => CloudProvider.UnregisterSafely())
            });

            steps.Add(new Step
            {
                Name = "InitSpaceFolders",
                Run = (token) => Task.Run(() => {
                    InitSpaceFolders(token);
                    logger.Info("InitSpaceFolders OK");
                }),
                Undo = () => Task.CompletedTask
            });

            steps.Add(new Step
            {
                Name = "ConnectCallbacks",
                Run = (token) => Task.Run(() => {
                    CloudProvider.ConnectCallbacks(configuration.root_path);
                    logger.Info("ConnectCallbacks OK");
                }),
                Undo = () => Task.Run(() => CloudProvider.DisconectCallbacks())
            });

            steps.Add(new Step
            {
                Name = "StartFileWatcher",
                Run = (token) => Task.Run(() => { 
                    watcher = new(configuration.root_path);
                    logger.Info("StartFileWatcher OK");
                }),
                Undo = () => Task.Run(() => { watcher?.Dispose(); })
            });

            return steps;
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

        private static void TestTokenValidity(CancellationToken token)
        {
            var task = RestClient.ExamineToken(token);
            task.Wait();
            TokenExamine te = task.Result;
            if (!te.isRestInterface())
            {
                throw new InvalidTokenType("Wrong token interface");
            }
        }

        private static void TestTokenAndOnezone(CancellationToken token)
        {
            InferTokenAccess(token);
            TestTokenValidity(token);
        }

        public static TokenAccess InferTokenAccess(CancellationToken token)
        {
            try
            {
                var taskTA = RestClient.InferAccessTokenScope(token);
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

        public static void InitSpaceFolders(CancellationToken token)
        {
            logger.Info("CREATING SPACE FOLDERS");
            using (PlaceholderCreateInfoList info = new())
            {
                TokenAccess tokenAccess = InferTokenAccess(token);
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
                        if (token.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(token);
                        }
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

                                var task5 = RestClient.GetFileAttribute(dirId, providerDomain, token);
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

                                spaceFolder = new(spaceName, fileInfo.file_id, space.Key, 
                                    new ProviderInfo(providerId, providerDomain), configuration.enableRefresh);
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

        public static void CreatePlaceholders(PlaceholderCreateInfoList info, string path)
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

        public static void InitSyncRootDir(CancellationToken token)
        {
            try
            {
                if (configuration.deleteExistingRootDir && Directory.Exists(configuration.root_path))
                {
                    Directory.Delete(configuration.root_path, true);
                }

                if (!Directory.Exists(configuration.root_path))
                {
                    _ = Directory.CreateDirectory(configuration.root_path);
                    logger.Info("Creating new SyncRoot Directory.");
                }

                // test root folder permissions
                File.Create(configuration.root_path + "testingAccess").Close();
                File.Delete(configuration.root_path + "testingAccess");

                if (Directory.EnumerateFileSystemEntries(configuration.root_path).Any())
                {
                    throw new RootFolderNotEmptyException("SyncRoot Directory must be empty.");
                }
                return;
            }
            catch (Exception e) when (e is UnauthorizedAccessException || e is IOException)
            {
                logger.Error($"Failed to create Root Folder - acess rights, {e}");

                throw new RootFolderAcessException("Insufficiend acess rights", e);
            }
        }
    }
}