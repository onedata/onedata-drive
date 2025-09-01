using Microsoft.VisualBasic.FileIO;
using NLog;
using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Vanara;
using Vanara.Collections;
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
        private CancellationTokenSource processingTokenSource;
        private AutoRefresh autoRefresh;
        private Task processingTask;

        public EventManager(AutoRefresh autoRefresh)
        {
            this.processingTokenSource = new();
            this.autoRefresh = autoRefresh;
            this.events = new List<Event>();
            this.processingTask = Task.Run(() => ProcessEvents(processingTokenSource.Token, autoRefresh.spaceFolder.name));
            Debug.Print($"Event Manager created: {autoRefresh.spaceFolder.name}");
        }

        public bool StopProcessing()
        {
            processingTokenSource.Cancel();
            try
            {
                Debug.Print("Waiting for event processing task to finish");
                processingTask.Wait();
                return true;
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                    {
                        Debug.Print("Event processing task was cancelled.");
                    }
                    else
                    {
                        Debug.Print("Event processing task encountered an error: {0}", e);
                        return false;
                    }
                }
                return true;
            }
        }

        public void AddEvent(FileEvent fileEvent)
        {
            Event newEvent = DetermineEventType(fileEvent);

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
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.Print("Cancellation requested, stopping event processing.");
                    break;
                }
                Debug.Print("Processing Task alive: {0}", spaceName);
                if (events.Count > 0)
                {
                    Event processedEvent = events[0];
                    events.RemoveAt(0);
                    string parentFolder = GetParentFolder(processedEvent.fileEvent);
                    Debug.Print("Processing event of type: {0}", processedEvent.type.ToString());
                    bool eventCompleted = false;
                    // Process the event based on its type
                    string filePath = Path.Combine(parentFolder, processedEvent.fileName ?? string.Empty);
                    bool directory = processedEvent.fileEvent.data.type == PlaceholderData.DIRECTORY;
                    switch (processedEvent.type)
                    {
                        case EventType.Updated:
                            CF_FS_METADATA metadata = Placeholders.CreateFSMetadata(processedEvent.fileAttribute, directory);
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
                                PlaceholderData placeholderData = new(
                                    processedEvent.fileAttribute.file_id,
                                    processedEvent.fileAttribute.name,
                                    processedEvent.fileAttribute.size,
                                    processedEvent.fileAttribute.atime,
                                    processedEvent.fileAttribute.mtime,
                                    processedEvent.fileAttribute.ctime);
                                createInfo.Add(Placeholders.CreateInfo(placeholderData));
                                CF_PLACEHOLDER_CREATE_INFO[] infoArr = createInfo.GetArray();
                                HRESULT hres = CfCreatePlaceholders(parentFolder, infoArr, (uint)infoArr.Length, CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out uint entriesProcessed);
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
                                Directory.Delete(filePath, true);
                                Debug.Print($"Directory Deleted: {processedEvent.fileEvent.fileId}" );
                                eventCompleted = true;
                            }
                            else
                            {
                                File.Delete(filePath);
                                Debug.Print($"File Deleted: {processedEvent.fileEvent.fileId}");
                                eventCompleted = true;
                            }
                            break;
                        default:
                            Debug.Print("Unknown event type");
                            break;
                    }
                    if (!eventCompleted)
                    {
                        Debug.Print($"Event {processedEvent.fileEvent.eventId} not completed, re-adding to the queue with penalty");
                        processedEvent.Penalize(5);
                        events.Add(processedEvent);
                    }
                }
                else
                {
                    Thread.Sleep(2000);
                }
            }
        }

        private void UpdatePlaceholderMetadata(CF_FS_METADATA metadata, string placeholderPath)
        {
            SafeHCFFILE? handle = null;
            try
            {
                HRESULT openHres = CfOpenFileWithOplock(placeholderPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_WRITE_ACCESS | CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, out handle);
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
        private CancellationTokenSource monitorTokenSource;
        private EventManager eventManager;
        private Task monitoringTask;
        public IReadOnlyList<string> MonitoredId => monitoredId.AsReadOnly();
        public IReadOnlyList<string> MonitoredPath => monitoredPath.AsReadOnly();
        public AutoRefresh(SpaceFolder spaceFolder)
        {
            this.monitoredId = new();
            this.monitoredPath = new();
            this.spaceFolder = spaceFolder;
            this.monitorTokenSource = new();
            this.eventManager = new EventManager(this);
            this.monitoringTask = Task.Run(() => MonitorFileEvents(monitorTokenSource.Token, out _));
            Debug.Print($"Autorefresh created: {spaceFolder.name}");
        }

        public void StopMonitoring()
        {
            monitorTokenSource.Cancel();
            eventManager.StopProcessing();
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
                monitorTokenSource.Cancel();
                monitorTokenSource = newCts;
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
                            long time = 0;
                            while (!readTask.IsCompleted)
                            {
                                if (time % 5 == 0)
                                {
                                    Debug.Print($"Waiting for read: {time}s");
                                }
                                Thread.Sleep(1000);
                                time += 1;
                            }
                            string line = readTask.Result ?? "NOTHING WAS READ";
                            Debug.Print($"READ LINE: {line}");
                            FileEvent fe = JsonSerializer.Deserialize<FileEvent>(line) ?? throw new Exception("Json Deserialize FAIL");
                            Task.Run(() => eventManager.AddEvent(fe));
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
