using Microsoft.VisualBasic.FileIO;
using NLog;
using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.Diagnostics;
using System.Text.Json;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;


namespace OnedataDrive
{
    internal class Event
    {
        internal EventType type;
        internal FileEvent fileEvent;
        internal string? fileName;
        internal FileAttribute? fileAttribute;
        internal int penaltyDurationSeconds { get; private set; }
        internal DateTime penaltyStartTime { get; private set; }
        private Event(FileEvent fileEvent, EventType eventType)
        {
            type = eventType;
            this.fileEvent = fileEvent;
            this.penaltyDurationSeconds = 0;

            this.fileName = null;
            this.fileAttribute = null;
        }

        public Event(FileEvent fileEvent, string fileName, EventType eventType = EventType.Unknown)
            : this(fileEvent, eventType)
        {
            this.fileName = fileName;
        }

        public Event(FileEvent fileEvent, string fileName, FileAttribute fileAttribute, EventType eventType = EventType.Unknown) 
            : this(fileEvent, eventType)
        {
            this.fileName = fileName;
            this.fileAttribute = fileAttribute;
        }

        public Event(FileEvent fileEvent, FileAttribute fileAttribute, EventType eventType = EventType.Unknown)
            : this(fileEvent, eventType)
        {
            this.fileAttribute = fileAttribute;
        }

        public void Penalize(int penalty)
        {
            this.penaltyDurationSeconds = penalty;
            this.penaltyStartTime = DateTime.UtcNow;
        }

        public bool IsPenalized()
        {
            if (DateTime.UtcNow.AddSeconds(-this.penaltyDurationSeconds) > this.penaltyStartTime)
            {
                this.penaltyDurationSeconds = 0;
                return false;
            }
            return true;
        }
    }

    internal class EventManager
    {
        public ThreadSafeList<Event> events;
        private CancellationTokenSource processingTokenSource;
        private AutoRefresh autoRefresh;
        private Task processingTask;

        public EventManager(AutoRefresh autoRefresh)
        {
            this.processingTokenSource = new();
            this.autoRefresh = autoRefresh;
            this.events = new ThreadSafeList<Event>();
            this.processingTask = Task.Run(() => ProcessEvents(processingTokenSource.Token, autoRefresh.spaceFolder.name));
            AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "CREATED", 
                filePath: autoRefresh.spaceFolder.name);
        }

        public bool StopProcessing()
        {
            AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "Stop processing", 
                filePath: autoRefresh.spaceFolder.name);
            processingTokenSource.Cancel();
            try
            {
                processingTask.Wait();
                AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "STOP OK", 
                    filePath: autoRefresh.spaceFolder.name);
                return true;
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                    {
                        AutoRefresh.logFormatter.LogFileOP(LogLevel.Warn, "EVENT MANAGER", "stopped/canceled OK", 
                            e, filePath: autoRefresh.spaceFolder.name);
                    }
                    else
                    {
                        AutoRefresh.logFormatter.LogFileOP(LogLevel.Error, "EVENT MANAGER", "STOP FAIL", 
                            e, filePath: autoRefresh.spaceFolder.name);
                        return false;
                    }
                }
                return true;
            }
        }

        public void AddEvent(FileEvent fileEvent)
        {
            try
            {
                Event newEvent = DetermineEventType(fileEvent);
                List<string> moreInfo = EventMoreInfo(newEvent);

                if (!events.Any(ev => ev.fileEvent.fileId == newEvent.fileEvent.fileId && ev.type > newEvent.type))
                {
                    events.RemoveAll(ev => ev.fileEvent.fileId == newEvent.fileEvent.fileId);
                    events.Add(newEvent);
                    AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "event added",
                        moreInfo: moreInfo, filePath: autoRefresh.spaceFolder.name);
                }
                else
                {
                    AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER",
                        "event not added - event with higher priority exists",
                        moreInfo: moreInfo, filePath: autoRefresh.spaceFolder.name);
                }
            }
            catch (Exception e)
            {
                AutoRefresh.logFormatter.LogFileOP(LogLevel.Error, "EVENT MANAGER", "Add event FAIL", e,
                    moreInfo: EventMoreInfo(fileEvent), filePath: autoRefresh.spaceFolder.name);
            }
            
        }

        public void ReAddEvent(Event newEvent)
        {
            List<string> moreInfo = EventMoreInfo(newEvent);
            if (events.Any(ev => ev.fileEvent.fileId == newEvent.fileEvent.fileId 
                && newEvent.type == EventType.Renamed && ev.type == EventType.Updated))
            {
                AutoRefresh.logFormatter.LogFileOP(LogLevel.Error, 
                    "EVENT MANAGER", "event not readded - not relevant anymore", 
                    moreInfo: moreInfo, filePath: autoRefresh.spaceFolder.name);
            }
            else if (!events.Any(ev => ev.fileEvent.fileId == newEvent.fileEvent.fileId 
                && ev.type > newEvent.type))
            {
                events.RemoveAll(ev => ev.fileEvent.fileId == newEvent.fileEvent.fileId);
                events.Add(newEvent);

                AutoRefresh.logFormatter.LogFileOP(LogLevel.Error, "EVENT MANAGER", "event readded",
                    moreInfo: moreInfo, filePath: autoRefresh.spaceFolder.name);
            }
            else 
            {
                AutoRefresh.logFormatter.LogFileOP(LogLevel.Error,
                    "EVENT MANAGER", "event not readded - event with higher priority already exists",
                    moreInfo: moreInfo, filePath: autoRefresh.spaceFolder.name);
            }
        }

        private Event DetermineEventType(FileEvent fileEvent)
        {
            string parentFolder = GetParentFolder(fileEvent);

            string? localFileName = null;
            localFileName = GetFileNameFromId(fileEvent.fileId, parentFolder, out string type);
            fileEvent.data.type = type;


            // test delete type
            List<ProviderInfo> providerInfos = autoRefresh.spaceFolder.providerInfos;
            FileAttribute fileAttribute;
            try
            {
                fileAttribute = RestClient.GetFileAttribute(fileEvent.fileId, providerInfos).Result;
            }
            catch (Exception ex)
            {
                AggregateException? ae = ex as AggregateException;
                if (ae is not null && ae.InnerExceptions.ToList().Any(e => e is NoSuchCloudFile))
                {
                    Debug.Print($"EventId: {fileEvent.eventId}, localFileName: {localFileName ?? ""}");
                    return new Event(fileEvent, localFileName ?? string.Empty, EventType.Deleted);
                }
                Debug.Print("Error in event handling: {0}", ex);
                throw;
            }

            // test update/rename/create type
            Debug.Print($"Parent Folder: {parentFolder}, file name: {fileAttribute.name}, eventId: {fileEvent.eventId}");
            string localId = string.Empty;
            try
            {
                string filePath = Path.Combine(parentFolder, fileAttribute.name);
                localId = PathUtils.GetPlaceholderId(filePath);
            }
            catch (FileNotFoundException) { }
            if (localId != string.Empty && localId == fileAttribute.file_id)
            {
                Debug.Print("File found");
                localFileName = fileAttribute.name;
                return new Event(fileEvent, localFileName, fileAttribute, EventType.Updated);
            }
            else
            {
                // get all file ids
                if (localFileName is not null)
                {
                    Debug.Print("File found by enumeration");
                    return new Event(fileEvent, localFileName, fileAttribute, EventType.Renamed);
                }
                return new Event(fileEvent, fileAttribute, EventType.Created);
            }
        }

        /// <summary>
        /// Retrieves the file name corresponding to the specified file ID within the given directory.
        /// </summary>
        /// <remarks>This method searches the specified directory for a file whose placeholder ID matches
        /// the provided <paramref name="fileId"/>. If a match is found, the file name is returned. If no match is
        /// found, the method returns <see langword="null"/>.</remarks>
        /// <param name="fileId">The unique identifier of the file to locate.</param>
        /// <param name="parentDirPath">The path of the directory to search for the file.</param>
        /// <returns>The name of the file if a file with the specified ID is found; otherwise, <see langword="null"/>.</returns>
        private string? GetFileNameFromId(string fileId, string parentDirPath, out string type)
        {
            foreach (string filePath in Directory.EnumerateFileSystemEntries(parentDirPath))
            {
                if (PathUtils.GetPlaceholderId(filePath) == fileId)
                {
                    if (Directory.Exists(filePath))
                    {
                        type = PlaceholderData.DIRECTORY;
                    }
                    else
                    {
                        type = PlaceholderData.REGULAR_FILE;
                    }
                    return PathUtils.GetLastInPath(filePath);
                }
            }
            type = string.Empty;
            return null;
        }

        private void ProcessEvents(CancellationToken cancellationToken, string spaceName)
        {
            while (true)
            {
                string opID = IdGenerator.GenerateId8();
                if (cancellationToken.IsCancellationRequested)
                {
                    AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "Process event cancel request", 
                        filePath: autoRefresh.spaceFolder.name, opID: opID);
                    break;
                }
                if (events.Count > 0)
                {
                    int index;
                    for (index = 0; index < events.Count; index++)
                    {
                        Event investigatedEvent = events[index];
                        if (investigatedEvent.IsPenalized())
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (index >= events.Count)
                    {
                        continue;
                    }
                    
                    Event processedEvent = events[index];
                    events.RemoveAt(index);

                    bool eventCompleted = ProcessEventWorker(processedEvent, opID);

                    if (!eventCompleted)
                    {
                        AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER",
                            "Event not processed - re-adding to the queue with penalty",
                            filePath: autoRefresh.spaceFolder.name, opID: opID);
                        processedEvent.Penalize(5);
                        ReAddEvent(processedEvent);
                    }
                    else
                    {
                        AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "Event processed",
                            filePath: autoRefresh.spaceFolder.name, opID: opID);
                    }
                }
                else
                {
                    cancellationToken.WaitHandle.WaitOne(2000);
                }
            }
            AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "Process event stopped",
                        filePath: autoRefresh.spaceFolder.name);
        }

        private bool ProcessEventWorker(Event processedEvent, string opID)
        {
            bool eventCompleted = false;
            List<string> moreInfo = EventMoreInfo(processedEvent);
            try
            {
                string parentFolder = GetParentFolder(processedEvent.fileEvent);
                moreInfo.Add($"ParentFolder: {parentFolder}");
                AutoRefresh.logFormatter.LogFileOP(LogLevel.Info, "EVENT MANAGER", "Processing event",
                    moreInfo: moreInfo, filePath: autoRefresh.spaceFolder.name, opID: opID);
                string filePath = Path.Combine(parentFolder, processedEvent.fileName ?? string.Empty);
                bool directory = processedEvent.fileEvent.data.type == PlaceholderData.DIRECTORY;
                switch (processedEvent.type)
                {
                    case EventType.Updated:
                        CF_FS_METADATA metadata = Placeholders.CreateFSMetadata(
                            processedEvent.fileAttribute, directory);
                        UpdatePlaceholderMetadata(metadata, filePath);
                        Debug.Print($"File Updated: {processedEvent.fileEvent.fileId}");
                        eventCompleted = true;
                        break;
                    case EventType.Renamed:
                        metadata = Placeholders.CreateFSMetadata(processedEvent.fileAttribute, directory);
                        UpdatePlaceholderMetadata(metadata, filePath);
                        if (directory)
                        {
                            FileSystem.RenameDirectory(filePath, processedEvent.fileAttribute!.name);
                        }
                        else
                        {
                            FileSystem.RenameFile(filePath, processedEvent.fileAttribute!.name);
                        }
                        Debug.Print($"File Renamed: {processedEvent.fileEvent.fileId}");
                        eventCompleted = true;
                        break;
                    case EventType.Created:
                        using (PlaceholderCreateInfo createInfo = new())
                        {
                            PlaceholderData placeholderData = new(processedEvent.fileAttribute);
                            createInfo.Add(Placeholders.CreateInfo(placeholderData));
                            CF_PLACEHOLDER_CREATE_INFO[] infoArr = createInfo.GetArray();
                            HRESULT hres = CfCreatePlaceholders(parentFolder, infoArr, (uint)infoArr.Length,
                                CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out uint entriesProcessed);
                            if (hres == HRESULT.S_OK || entriesProcessed == infoArr.Length)
                            {
                                eventCompleted = true;
                                Debug.Print($"File Created: {processedEvent.fileEvent.fileId}");
                            }
                        }
                        break;
                    case EventType.Deleted:
                        if (processedEvent.fileName is null)
                        {
                            eventCompleted = true;
                            Debug.Print("Event Delete: file can not be found. Event completed.");
                            break;
                        }
                        if (directory)
                        {
                            if (Directory.Exists(filePath))
                            {
                                Directory.Delete(filePath, true);
                            }
                            Debug.Print($"Directory Deleted: {processedEvent.fileEvent.fileId}");
                            eventCompleted = true;
                        }
                        else
                        {
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                            }
                            Debug.Print($"File Deleted: {processedEvent.fileEvent.fileId}");
                            eventCompleted = true;
                        }
                        break;
                    default:
                        Debug.Print("Unknown event type");
                        break;
                }

            }
            catch (Exception e)
            {
                AutoRefresh.logFormatter.LogFileOP(LogLevel.Error, "EVENT MANAGER", "Process event error", e,
                    filePath: autoRefresh.spaceFolder.name, opID: opID);
            }
            
            return eventCompleted;
        }

        private void UpdatePlaceholderMetadata(CF_FS_METADATA metadata, string placeholderPath)
        {
            SafeHCFFILE? handle = null;
            try
            {
                HRESULT openHres = CfOpenFileWithOplock(placeholderPath, 
                    CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS | CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, 
                    out handle);
                if (openHres != HRESULT.S_OK)
                {
                    throw new Exception($"CfOpenFileWithOplock HRES number: {((int)openHres)}" +
                        $"\n HRES text: {openHres}");
                }
                long updateUsn = 0;
                HRESULT hres = CfUpdatePlaceholder(FileHandle: handle.DangerousGetHandle(),
                                    FsMetadata: metadata,
                                    FileIdentity: 0,
                                    FileIdentityLength: 0,
                                    DehydrateRangeCount: 0,
                                    UpdateFlags: CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC,
                                    UpdateUsn: ref updateUsn
                                    );
                if (hres != HRESULT.S_OK)
                {
                    throw new Exception($"CfUpdatePlaceholder HRES number: {((int)hres)}" +
                        $"\n HRES text: {hres}");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (handle != null)
                {
                    CfCloseHandle(handle);
                }
            }
        }

        private string GetParentFolder(FileEvent fileEvent)
        {
            return autoRefresh.monitoredPath[autoRefresh.monitoredId.IndexOf(fileEvent.parentFileId)];
        }

        private List<string> EventMoreInfo(FileEvent fileEvent)
        {
            return new List<string> {
                $"EventId: {fileEvent.eventId}",
                $"EventType: {fileEvent.eventType}",
                $"FileId: {fileEvent.fileId}",
                $"ParentId: {fileEvent.parentFileId}" };
        }

        private List<string> EventMoreInfo(Event fileEvent)
        {
            List<string> moreInfo = EventMoreInfo(fileEvent.fileEvent);
            moreInfo.Add($"FileName: {fileEvent.fileName}");
            moreInfo.Add($"EventCategory: {fileEvent.type}");
            return moreInfo;
        }
    }

    internal enum EventType
    {
        Unknown = 0,
        Updated = 1,
        Renamed = 2,
        Created = 3,
        Deleted = 4
    }

    public class AutoRefresh
    {
        internal static Logger logger = LogManager.GetCurrentClassLogger();
        internal static LoggerFormater logFormatter = new(logger);

        internal SpaceFolder spaceFolder;
        internal List<string> monitoredId;
        internal List<string> monitoredPath;
        private CancellationTokenSource masterTokenSource;
        private CancellationTokenSource monitorTokenSource;
        private EventManager eventManager;
        private uint restartNeeded;
        private Task monitoringTask;
        private Task restartCheckerTask;
        public AutoRefresh(SpaceFolder spaceFolder)
        {
            this.masterTokenSource = new();
            this.monitorTokenSource = CancellationTokenSource.CreateLinkedTokenSource(masterTokenSource.Token);

            this.spaceFolder = spaceFolder;
            this.monitoredId = new();
            this.monitoredPath = new();
            this.restartNeeded = 0;
            this.eventManager = new EventManager(this);

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
            if (!monitoredId.Contains(fileId))
            {
                monitoredId.Add(fileId);
                monitoredPath.Add(path);
                logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Added to monitor", 
                    moreInfo: moreInfo, filePath: spaceFolder.name, opID: id);
                Interlocked.Increment(ref restartNeeded);
            }
            else
            {
                logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Add to monitor - already contains", 
                    moreInfo: moreInfo, filePath: spaceFolder.name, opID: id);
            }
        }

        private void RestartChecker()
        {
            while (!masterTokenSource.Token.IsCancellationRequested)
            {
                if (restartNeeded > 0)
                {
                    RestartMonitoring();
                }
                masterTokenSource.Token.WaitHandle.WaitOne(4000);
            }
            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Restart checker - stop", 
                filePath: spaceFolder.name);
        }

        private void RestartMonitoring()
        {
            string opID = IdGenerator.GenerateId8();
            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Monitoring task handover - START", 
                moreInfo: monitoredPath, filePath: spaceFolder.name, opID: opID);
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
                        moreInfo: monitoredPath, filePath: spaceFolder.name, opID: opID);
                    return;
                }
                if (newMonitoringTask.IsFaulted)
                {
                    newCts.Cancel();
                    logFormatter.LogFileOP(LogLevel.Error, "AUTOREFRESH", "Monitoring task handover - new task faulted", 
                        moreInfo: monitoredPath, filePath: spaceFolder.name, opID: opID);
                    return;
                }
            }
            if (!connected)
            {
                newCts.Cancel();
                logFormatter.LogFileOP(LogLevel.Error, "AUTOREFRESH", "Monitoring task handover - timeout", 
                    moreInfo: monitoredPath, filePath: spaceFolder.name, opID: opID);
                return;
            }
            monitorTokenSource.Cancel();
            monitorTokenSource = newCts;
            monitoringTask = newMonitoringTask;
            Interlocked.Decrement(ref restartNeeded);
            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Monitoring task handover - OK", 
                filePath: spaceFolder.name, opID: opID);
        }

        private void MonitorFileEvents(CancellationToken cancelToken, out bool connected)
        {
            string opID = IdGenerator.GenerateId8();
            connected = false;
            if (monitoredId.Count <= 0)
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
                    Task<Stream> connectionTask = RestClient.GetFileEventStream(monitoredId, providerInfos, spaceId);
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
            string lineRead = "";
            using (Stream stream = connectionTask.Result)
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    while (true)
                    {
                        try
                        {
                            Task<string?> readTask = reader.ReadLineAsync(cancelToken).AsTask();
                            connected = true;
                            int time = 0;
                            while (!readTask.IsCompleted)
                            {
                                if (time % 10 == 0 && !cancelToken.IsCancellationRequested)
                                {
                                    Debug.Print($"ReadMonitorStream - waiting to read");
                                    time = 0;
                                }
                                cancelToken.WaitHandle.WaitOne(1000);
                                time += 1;
                            }
                            if (cancelToken.IsCancellationRequested)
                            {
                                logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Read monitor stream - Cancel Requested", 
                                    filePath: spaceFolder.name, opID: opID);
                                break;
                            }
                            lineRead = readTask.Result ?? "NOTHING WAS READ";
                            Debug.Print($"READ LINE: {lineRead}");
                            FileEvent fe = JsonSerializer.Deserialize<FileEvent>(lineRead) ?? throw new Exception("Json Deserialize FAIL");
                            Task.Run(() => eventManager.AddEvent(fe));
                            string json = JsonSerializer.Serialize(fe);
                            Debug.Print("JSON: {0}", json);
                        }
                        catch (Exception e)
                        {
                            List<string> errString = new() { "Read line: " + lineRead };
                            if (connectionTask.IsCompleted && !cancelToken.IsCancellationRequested)
                            {
                                logFormatter.LogFileOP(LogLevel.Error, "AUTOREFRESH", "Read monitor stream - Aborted", 
                                    e, errString, filePath: spaceFolder.name, opID: opID);
                                return;
                            }
                            logFormatter.LogFileOP(LogLevel.Error, "AUTOREFRESH", "Read monitor stream - Error", 
                                e, errString, filePath: spaceFolder.name, opID: opID);
                        }
                        if (cancelToken.IsCancellationRequested)
                        {
                            logFormatter.LogFileOP(LogLevel.Info, "AUTOREFRESH", "Read monitor stream - Cancel Requested", 
                                filePath: spaceFolder.name, opID: opID);
                            break;
                        }
                    }
                }
            }
        }


    }
}
