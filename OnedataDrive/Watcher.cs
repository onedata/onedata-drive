using NLog;
using OnedataDrive.Utils;

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

        public Watcher(string rootDir)
        {
            this.watcher = new(rootDir) {
                InternalBufferSize = 64 * 1024 // max recomended size
            };

            this.logger = LogManager.GetCurrentClassLogger();
            this.loggerFormater = new(logger);

            this.eventProcessor = new WatcherEventProcessor(logger);
            this.bufferedEventMerger = new(eventProcessor, 5, logger);

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

    }
}