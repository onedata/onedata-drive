using NLog;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.Diagnostics;
using System.Text.Json;


namespace OnedataDrive
{
    public class AutoRefresh
    {
        public class MonitoredFolder
        {
            public string id;
            public string path;
            public MonitoredFolder(string id, string path)
            {
                this.id = id;
                this.path = path;
            }
        }

        public Logger logger;
        internal LoggerFormater logFormatter;

        internal SpaceFolder spaceFolder;
        internal ThreadSafeMonitored monitored;
        private CancellationTokenSource masterTokenSource;
        private CancellationTokenSource monitorTokenSource;
        private FileEventProcessor eventManager;
        private BufferedEventMerger<FileEvent> eventMerger;
        private bool restartNeeded;
        private Task monitoringTask;
        private Task restartCheckerTask;
        public AutoRefresh(SpaceFolder spaceFolder)
        {
            this.logger = LogManager.GetCurrentClassLogger();
            this.logFormatter = new(logger);

            this.masterTokenSource = new();
            this.monitorTokenSource = CancellationTokenSource.CreateLinkedTokenSource(masterTokenSource.Token);

            this.spaceFolder = spaceFolder;
            this.monitored = new();
            this.restartNeeded = false;
            this.eventManager = new FileEventProcessor(this);
            this.eventMerger = new BufferedEventMerger<FileEvent>(eventManager, 15, logger);

            this.monitoringTask = Task.Run(() => MonitorFileEvents(monitorTokenSource.Token, out _));
            this.restartCheckerTask = Task.Run(() => RestartChecker());
            
            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "CREATED", filePath: spaceFolder.name);
        }

        public void StopMonitoring()
        {
            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Stop monitoring", filePath: spaceFolder.name);
            masterTokenSource.Cancel();
            eventManager.StopProcessing();
            try
            {
                monitoringTask.Wait();
                restartCheckerTask.Wait();
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                    {
                        logFormatter.LogFileOP(LogLevel.Warn, "AUTOREFRESH", "stopped/canceled OK", 
                            e, filePath: spaceFolder.name);
                    }
                    else
                    {
                        logFormatter.LogFileOP(LogLevel.Error, "AUTOREFRESH", "STOP FAIL", 
                            e, filePath: spaceFolder.name);
                        return;
                    }
                }
            }
            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "STOP OK", filePath: spaceFolder.name);
        }

        public void AddToMonitored(string fileId, string path)
        {
            string id = IdGenerator.GenerateId8();
            List<string> moreInfo = new() { $"FileId: {fileId}", $"Path: {path}" };
            if (monitored.AddToMonitored(fileId, path))
            {
                logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Added to monitor", 
                    moreInfo: moreInfo, filePath: spaceFolder.name, opID: id);
                restartNeeded = true;
            }
            else
            {
                logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Add to monitor - already contains", 
                    moreInfo: moreInfo, filePath: spaceFolder.name, opID: id);
            }
        }

        public void RemoveFromMonitored(string fileId)
        {
            string id = IdGenerator.GenerateId8();
            List<string> moreInfo = new() { $"FileId: {fileId}" };
            if (monitored.RemoveFromMonitored(fileId))
            {
                logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Removed from monitor", 
                    moreInfo: moreInfo, filePath: spaceFolder.name, opID: id);
                restartNeeded = true;
            }
            else
            {
                logFormatter.LogFileOP(LogLevel.Warn, "AUTOREFRESH", "Remove from monitor failed", 
                    moreInfo: moreInfo, filePath: spaceFolder.name, opID: id);
            }
        }

        public void RenameMonitored(string fileId, string newName, string opID = "")
        {   
            if (string.IsNullOrEmpty(opID))
            {
                opID = IdGenerator.GenerateId8();
            }
            string space = "SPC: " + spaceFolder.name;
            List<string> renamed = monitored.RenameMonitored(fileId, newName);
            if (renamed.Count <= 0)
            {
                logFormatter.LogFileOP(LogLevel.Warn, "AUTOREFRESH", "Rename monitored - not found", 
                    moreInfo: new List<string> { $"FileId: {fileId}", $"NewName: {newName}" }, 
                    filePath: space, opID: opID);
            }
            else
            {
                logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Renamed monitored", 
                    moreInfo: renamed, filePath: space, opID: opID);
            }
        }

        private void RestartChecker()
        {
            while (!masterTokenSource.Token.IsCancellationRequested)
            {
                if (restartNeeded)
                {
                    try
                    {
                        restartNeeded = false;
                        RestartMonitoring();
                    }
                    catch (Exception)
                    {
                        restartNeeded = true;
                    }
                }
                masterTokenSource.Token.WaitHandle.WaitOne(4000);
            }
            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Restart checker - stop", 
                filePath: spaceFolder.name);
        }

        private void RestartMonitoring()
        {
            string opID = IdGenerator.GenerateId8();
            List<string> allPaths = monitored.Select(m => m.path).ToList();

            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Monitoring task handover - START", 
                moreInfo: allPaths, filePath: spaceFolder.name, opID: opID);
            CancellationToken masterToken = masterTokenSource.Token;
            const int sleepMS = 500;
            const int timeout = 30 * sleepMS;
            int clock = 0;

            CancellationTokenSource newCts = CancellationTokenSource.CreateLinkedTokenSource(masterTokenSource.Token);
            bool connected = false;
            Task newMonitoringTask = Task.Run(() => MonitorFileEvents(newCts.Token, out connected));
            while (!connected && clock < timeout)
            {
                masterToken.WaitHandle.WaitOne(sleepMS);
                clock += sleepMS;
                if (masterToken.IsCancellationRequested)
                {
                    newCts.Cancel();
                    logFormatter.LogFileOP(LogLevel.Error, "AUTOREFRESH", "Monitoring task handover - cancelled", 
                        moreInfo: allPaths, filePath: spaceFolder.name, opID: opID);
                    return;
                }
                if (newMonitoringTask.IsFaulted)
                {
                    newCts.Cancel();
                    logFormatter.LogFileOP(LogLevel.Error, "AUTOREFRESH", "Monitoring task handover - new task faulted", 
                        moreInfo: allPaths, filePath: spaceFolder.name, opID: opID);
                    return;
                }
            }
            if (!connected)
            {
                newCts.Cancel();
                logFormatter.LogFileOP(LogLevel.Error, "AUTOREFRESH", "Monitoring task handover - timeout", 
                    moreInfo: allPaths, filePath: spaceFolder.name, opID: opID);
                return;
            }
            monitorTokenSource.Cancel();
            monitorTokenSource = newCts;
            monitoringTask = newMonitoringTask;
            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Monitoring task handover - OK", 
                filePath: spaceFolder.name, opID: opID);
        }

        private void MonitorFileEvents(CancellationToken cancelToken, out bool connected)
        {
            string opID = IdGenerator.GenerateId8();
            connected = false;
            if (monitored.Count <= 0)
            {
                logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Monitor Empty", 
                    filePath: spaceFolder.name, opID: opID);
                return;
            }

            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Monitor Started", 
                filePath: spaceFolder.name, opID: opID);

            string spaceId = spaceFolder.spaceId;
            List<ProviderInfo> providerInfos = spaceFolder.providerInfos;

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    List<ObservedAttribute> observedAttributes = new()
                    {
                        ObservedAttribute.type,
                        ObservedAttribute.mtime,
                        ObservedAttribute.size,
                        ObservedAttribute.name
                    };
                    Task<Stream> connectionTask = RestClient.GetFileEventStream(monitored.Select(x => x.id).ToList(), 
                        providerInfos, spaceId, observedAttributes);
                    connectionTask.Wait();
                    ReadMonitorStream(cancelToken, ref connected, connectionTask);
                }
                catch (Exception e)
                {
                    logFormatter.LogFileOP(LogLevel.Error, "AUTOREFRESH", "Monitor Error",
                        e, filePath: spaceFolder.name, opID: opID);
                    cancelToken.WaitHandle.WaitOne(10000);
                }
            }
            
            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Monitor Stopped", 
                filePath: spaceFolder.name, opID: opID);
        }

        private void ReadMonitorStream(CancellationToken cancelToken, ref bool connected, Task<Stream> connectionTask)
        {
            string opID = IdGenerator.GenerateId8();
            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Read monitor stream - Started",
                filePath: spaceFolder.name, opID: opID);

            using (Stream stream = connectionTask.Result)
            {
                connected = true;
                ReadStream(cancelToken, stream, opID).Wait();
            }
                
        }

        private async Task ReadStream(CancellationToken cancelToken, Stream stream, string opID)
        {
            try
            {
                await foreach (SseEvent receivedEvent in SseReader.Read(stream, cancelToken))
                {
                    //Debug.Print("Event received: {0}", receivedEvent.ToString());
                    FileEvent newEvent = new FileEvent(receivedEvent);
                    eventMerger.AddEvent(newEvent);
                    //string json = JsonSerializer.Serialize(newEvent);
                    //Debug.Print("JSON: {0}", json);
                }
            }
            catch (OperationCanceledException)
            {
                logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Read monitor stream - Cancel Requested",
                                filePath: spaceFolder.name, opID: opID);
            }
        }
    }
}
