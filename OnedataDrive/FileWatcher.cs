using NLog;
using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
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
        private BufferedEventMerger<WatcherEvent> bufferedEventMerger;
        private WatcherEventProcessor eventProcessor;

        public FileWatcher(string rootDir)
        {
            this.watcher = new(rootDir);
            this.logger = LogManager.GetCurrentClassLogger();
            this.loggerFormater = new(logger);

            this.eventProcessor = new WatcherEventProcessor(logger);
            this.bufferedEventMerger = new(eventProcessor, 5, logger);

            this.watcher.NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.LastWrite;

            this.watcher.Created += new FileSystemEventHandler(OnEvent);
            this.watcher.Changed += new FileSystemEventHandler(OnEvent);
            this.watcher.Error += new ErrorEventHandler(OnError);

            this.watcher.IncludeSubdirectories = true;
            this.watcher.EnableRaisingEvents = true;

            this.disposed = false;
        }

        public void OnError(object sender, ErrorEventArgs e)
        {
            loggerFormater.LogFileOP(LogLevel.Error, "FILE WATCHER", "ERROR event", e.GetException());
        }

        public void OnEvent(object sender, FileSystemEventArgs e)
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