using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.Diagnostics;

using System.Text.Json;


namespace OnedataDrive
{
    internal class Event
    {
        internal EventType type;
        internal FileEvent fileEvent;
        internal string? fileName;
        internal FileAttribute? fileAttribute;
        internal int penalty;
        // timestamp
        private Event(FileEvent fileEvent, EventType eventType)
        {
            type = eventType;
            this.fileEvent = fileEvent;
            this.penalty = 0;

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
            this.penalty += penalty;
        }
    }

    internal class EventManager
    {
        public List<Event> events;
        private CancellationToken cancellationToken;

        public EventManager(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            events = new List<Event>();
        }

        public void AddEvent(FileEvent fileEvent, AutoRefresh autoRefresh)
        {
            Event newEvent = DetermineEventType(fileEvent, autoRefresh);

            if (!events.Any(ev => ev.fileEvent.fileId == newEvent.fileEvent.fileId && ev.type > newEvent.type))
            {
                events.RemoveAll(ev => ev.fileEvent.fileId == newEvent.fileEvent.fileId);
                events.Add(newEvent);
                Debug.Print("EVENT ADDED, type {0}", newEvent.type.ToString());
            }
            else
            {
                Debug.Print($"EVENT NOT ADDED, type {newEvent.type.ToString()} \n Event ignored, " +
                    $"because there already is event with higher priority (making new event redundant)");
            }
        }

        public void ReAddEvent(Event newEvent)
        {
            if (events.Any(ev => ev.fileEvent.fileId == newEvent.fileEvent.fileId 
                && newEvent.type == EventType.Renamed && ev.type == EventType.Updated))
            {
                Debug.Print($"EVENT NOT READDED, type {newEvent.type.ToString()} \n Event ignored, " +
                    $"because Rename event is not relevant anymore)");
            }
            else if (!events.Any(ev => ev.fileEvent.fileId == newEvent.fileEvent.fileId 
                && ev.type > newEvent.type))
            {
                events.RemoveAll(ev => ev.fileEvent.fileId == newEvent.fileEvent.fileId);
                events.Add(newEvent);
                Debug.Print("EVENT ADDED, type {0}", newEvent.type.ToString());
            }
            else 
            {
                Debug.Print($"EVENT NOT READDED, type {newEvent.type.ToString()} \n Event ignored, " +
                    $"because there already is event with higher priority (making new event redundant)");
            }
        }

        private Event DetermineEventType(FileEvent fileEvent, AutoRefresh autoRefresh)
        {
            string? localFileName = null;
            string parentFolder = autoRefresh.monitoredPath[autoRefresh.monitoredId.IndexOf(fileEvent.parentFileId)];
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
                    Debug.Print($"EventId: {fileEvent.eventId}");
                    localFileName = GetFileNameFromId(fileEvent.fileId, parentFolder);
                    return new Event(fileEvent, localFileName ?? string.Empty, EventType.Deleted);
                }
                Debug.Print("Error in event handling: {0}", ex);
                throw;
            }

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
                localFileName = GetFileNameFromId(fileEvent.fileId, parentFolder);
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
        private string? GetFileNameFromId(string fileId, string parentDirPath)
        {
            foreach (string filePath in Directory.EnumerateFiles(parentDirPath))
            {
                if (PathUtils.GetPlaceholderId(filePath) == fileId)
                {
                    Debug.Print("File found by enumeration");
                    return PathUtils.GetLastInPath(filePath);
                }
            }
            return null;
        }

        private void ProcessEvents(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.Print("Cancellation requested, stopping event processing.");
                    break;
                }
                if (events.Count > 0)
                {
                    Event precessedEvent = events[0];
                    events.RemoveAt(0);
                    Debug.Print("Processing event of type: {0}", precessedEvent.type.ToString());
                    bool eventCompleted = false;
                    // Process the event based on its type

                    switch (precessedEvent.type)
                    {
                        case EventType.Updated:
                            // ok
                            Debug.Print($"File Updated: {precessedEvent.fileEvent.fileId}");
                            break;
                        case EventType.Renamed:
                            // ok
                            Debug.Print($"File Renamed: {precessedEvent.fileEvent.fileId}");
                            break;
                        case EventType.Created:
                            // ok
                            Debug.Print($"File Created: {precessedEvent.fileEvent.fileId}");
                            break;
                        case EventType.Deleted:
                            if (precessedEvent.fileName is null)
                            {
                                eventCompleted = true;
                                Debug.Print("Event Delete: file can not be found. Event completed.");
                            }
                            Debug.Print($"File Deleted: {precessedEvent.fileEvent.fileId}");
                            break;
                        default:
                            Debug.Print("Unknown event type");
                            break;
                    }
                    if (!eventCompleted)
                    {
                        precessedEvent.Penalize(5);
                        events.Add(precessedEvent);
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
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
        internal SpaceFolder spaceFolder;
        internal List<string> monitoredId;
        internal List<string> monitoredPath;
        private CancellationTokenSource cts;
        private EventManager eventManager;
        private Task monitoringTask;
        public IReadOnlyList<string> MonitoredId => monitoredId.AsReadOnly();
        public IReadOnlyList<string> MonitoredPath => monitoredPath.AsReadOnly();
        public AutoRefresh(SpaceFolder spaceFolder)
        {
            this.monitoredId = new();
            this.monitoredPath = new();
            this.spaceFolder = spaceFolder;
            this.cts = new();
            this.eventManager = new EventManager(cts.Token);
            this.monitoringTask = Task.Run(() => MonitorFileEvents(cts.Token, out _));
        }

        public void StopMonitoring()
        {
            cts.Cancel();
            try
            {
                Debug.Print("Waiting for task to finish");
                monitoringTask.Wait();
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                    {
                        Debug.Print("Monitoring task was cancelled.");
                    }
                    else
                    {
                        Debug.Print("Monitoring task encountered an error: {0}", e);
                    }
                }
            }
            Debug.Print("Monitoring task has been stopped.");
        }

        public void AddToMonitored(string fileId, string path)
        {
            if (!monitoredId.Contains(fileId))
            {
                monitoredId.Add(fileId);
                monitoredPath.Add(path);
                Debug.Print($"Added {fileId} to monitored list. Path: {path}");
                CancellationTokenSource newCts = new();
                bool connected = false;
                Task newMonitoringTask = Task.Run(() => MonitorFileEvents(newCts.Token, out connected));
                while (!connected)
                {
                    Debug.Print("Waiting for connection to be established...");
                    Thread.Sleep(500);
                }
                cts.Cancel();
                cts = newCts;
                monitoringTask = newMonitoringTask;
                Debug.Print("TASK HANDOVER");
            }
            else
            {
                Debug.Print($"{fileId} is already in the monitored list.");
            }
        }

        private void MonitorFileEvents(CancellationToken cancelToken, out bool connected)
        {
            connected = false;
            if (monitoredId.Count <= 0)
            {
                Debug.Print("No files to monitor. Exiting monitoring task.");
                return;
            }

            string spaceId = spaceFolder.spaceId;
            List<ProviderInfo> providerInfos = spaceFolder.providerInfos;
            Task<Stream> task = RestClient.GetFileEventStream(monitoredId, providerInfos, spaceId);
            task.Wait();
            using (Stream stream = task.Result)
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    while (true)
                    {
                        try
                        {
                            Task<string?> readTask = reader.ReadLineAsync(cancelToken).AsTask();
                            connected = true;
                            double time = 0;
                            while (!readTask.IsCompleted)
                            {
                                Debug.Print($"Waiting for read: {time}s");
                                Thread.Sleep(500);
                                time += 0.5;
                            }
                            string line = readTask.Result ?? "NOTHING WAS READ";
                            Debug.Print($"READ LINE: {line}");
                            FileEvent fe = JsonSerializer.Deserialize<FileEvent>(line) ?? throw new Exception("Json Deserialize FAIL");
                            Task.Run(() => eventManager.AddEvent(fe, this));
                            string json = JsonSerializer.Serialize(fe);
                            Debug.Print("JSON: {0}", json);
                        }
                        catch (Exception e) when (e is OperationCanceledException || e is ObjectDisposedException)
                        {
                            Debug.Print("Read operation was cancelled: {0}", e);
                            break;
                        }
                        catch (Exception e)
                        {
                            Debug.Print("Error reading line: {0}", e.Message);
                            break;
                        }
                        if (cancelToken.IsCancellationRequested)
                        {
                            Debug.Print("Cancellation requested, stopping processing.");
                            break;
                        }
                    }
                    Debug.Print("Finished reading stream.");
                }
            }
            
        }
    }
}
