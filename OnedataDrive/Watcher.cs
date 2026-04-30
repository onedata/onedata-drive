using NLog;
using OnedataDrive.Utils;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.Kernel32;

namespace OnedataDrive
{
    public class Watcher
    {
        private FileSystemWatcher watcher;
        private bool disposed = true;
        private Logger logger;
        private LoggerFormater loggerFormater;
        private BufferedEventMerger<WatcherEvent> bufferedEventMerger;
        private WatcherEventProcessor eventProcessor;
        private Crawler crawler;

        public Watcher(string rootDir)
        {
            this.watcher = new(rootDir) {
                InternalBufferSize = 64 * 1024 // max recomended size
            };

            this.logger = LogManager.GetCurrentClassLogger();
            this.loggerFormater = new(logger);

            this.eventProcessor = new WatcherEventProcessor(logger);
            this.bufferedEventMerger = new(eventProcessor, 5, logger);

            this.crawler = new(this);

            this.watcher.NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.FileName
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.LastWrite;

            this.watcher.Created += new FileSystemEventHandler(OnCreate);
            this.watcher.Changed += new FileSystemEventHandler(OnChange);
            this.watcher.Renamed += new RenamedEventHandler(OnRename);
            this.watcher.Error += new ErrorEventHandler(OnError);

            this.watcher.IncludeSubdirectories = true;
            this.watcher.EnableRaisingEvents = true;

            this.disposed = false;
        }

        public void OnError(object sender, ErrorEventArgs e)
        {
            loggerFormater.LogFileOP(LogLevel.Error, "FILE WATCHER", "ERROR event", e.GetException());
            crawler.Run();
        }

        public void OnCreate(object sender, FileSystemEventArgs e)
        {
            WatcherEvent watcherEvent = new(sender, e);
            bufferedEventMerger.AddEvent(watcherEvent);
        }

        public void OnChange(object sender, FileSystemEventArgs e)
        {
            WatcherEvent watcherEvent = new(sender, e);
            bufferedEventMerger.AddEvent(watcherEvent);
        }

        public void OnRename(object sender, RenamedEventArgs e)
        {
            WatcherEvent watcherEvent = new(sender, e);
            bufferedEventMerger.AddEvent(watcherEvent);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                crawler.Stop();
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                bufferedEventMerger.Dispose();
                eventProcessor.StopProcessing();
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

        internal class Crawler
        {
            private readonly Watcher PARENT;
            private Task crawlerTask;
            private CancellationTokenSource masterToken;
            private CancellationTokenSource? linkedToken;
            
            public Crawler(Watcher watcher)
            {
                this.PARENT = watcher;
                this.crawlerTask = Task.CompletedTask;
                this.masterToken = new CancellationTokenSource();
            }

            public void Run()
            {
                if (masterToken.IsCancellationRequested)
                {
                    throw new ObjectDisposedException("Crawler has been stopped and cannot be restarted.");
                }
                if (!crawlerTask.IsCompleted)
                {
                    linkedToken?.Cancel();
                    try
                    {
                        crawlerTask.Wait(masterToken.Token);
                    }
                    catch (Exception) { }
                }
                if (!masterToken.IsCancellationRequested)
                {
                    PARENT.loggerFormater.LogFileOP(LogLevel.Info, "CRAWLER", "Starting crawler task.");
                    linkedToken = CancellationTokenSource.CreateLinkedTokenSource(masterToken.Token);
                    crawlerTask = Task.Run(() => DirectoryCrawler(linkedToken.Token));
                }
            }

            public void Stop()
            {
                masterToken.Cancel();
            }

            private void DirectoryCrawler(CancellationToken token)
            {
                int counterFile = 0;
                int counterDir = 0;
                try
                {
                    Queue<string> directoriesToProcess = new();
                    directoriesToProcess.Enqueue(PARENT.watcher.Path);

                    while (directoriesToProcess.Count > 0)
                    {
                        token.ThrowIfCancellationRequested();
                        string dirPath = directoriesToProcess.Dequeue();

                        foreach (WIN32_FIND_DATA data in EnumDirectory(dirPath))
                        {
                            bool isDirectory = ((data.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory);
                            CF_PLACEHOLDER_STATE state = CfGetPlaceholderStateFromFindData(data);
                            if ((state & CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC) != CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC)
                            {
                                WatcherEvent watcherEvent = new(this, new FileSystemEventArgs(WatcherChangeTypes.Changed, dirPath, data.cFileName));
                                PARENT.bufferedEventMerger.AddEvent(watcherEvent);
                                if (isDirectory)
                                {
                                    counterDir++;
                                }
                                else
                                {
                                    counterFile++;
                                }
                            }
                            if (isDirectory)
                            {
                                directoriesToProcess.Enqueue(Path.Combine(dirPath, data.cFileName));
                            }
                        }
                    }
                    PARENT.loggerFormater.LogFileOP(LogLevel.Debug, "CRAWLER", $"Crawler finished. Found {counterFile} files and {counterDir} directories not in sync.");
                }
                catch (Exception e)
                {
                    if (e is not OperationCanceledException)
                    {
                        PARENT.loggerFormater.LogFileOP(LogLevel.Error, "CRAWLER", "Error during crawling", e); 
                    }
                    throw;
                }
            }

            private static IEnumerable<WIN32_FIND_DATA> EnumDirectory(string path)
            {
                using SafeSearchHandle hFind = FindFirstFile(Path.Combine(path, "*"), out var findData);
                if (hFind.IsInvalid) yield break;

                do
                {
                    if (findData.cFileName != "." && findData.cFileName != "..")
                        yield return findData;
                } while (FindNextFile(hFind, out findData));
            }
        }
    }
}