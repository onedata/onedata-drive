using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using OnedataDrive.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.ApplicationModel.Contacts;

namespace OnedataDrive
{
    internal class Event
    {
        internal EventType type;
        internal FileEvent fileEvent;
        // timestamp
        public Event(FileEvent fileEvent, EventType eventType = EventType.Unknown)
        {
            type = eventType;
            this.fileEvent = fileEvent;
        }
    }

    internal class EventManager
    {
        public List<Event> events;

        public EventManager()
        {
            events = new List<Event>();
        }

        public void AddEvent(FileEvent fileEvent, AutoRefresh autoRefresh)
        {
            Event e = new Event(fileEvent);

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
                    e.type = EventType.FileDeleted;
                    events.Add(e);
                    Debug.Print("EVENT ADDED, type {0}", e.type.ToString());
                    return;
                }
                Debug.Print("Error in event handling: {0}", ex);
                throw;
            }
            

            string parentFolder = autoRefresh.monitoredPath[autoRefresh.monitoredId.IndexOf(fileEvent.parentFileId)];
            string filePath = System.IO.Path.Combine(parentFolder, fileAttribute.name);

            try
            {
                string localId = PathUtils.GetPlaceholderId(filePath);
                if (localId == fileAttribute.file_id)
                {
                    Debug.Print("File found");
                    e.type = EventType.Updated;
                }
                else
                {
                    // get all file ids
                    foreach (string fileName in Directory.EnumerateFiles(parentFolder))
                    {
                        if (PathUtils.GetPlaceholderId(fileName) == fileEvent.fileId)
                        {
                            Debug.Print("File found by enumeration");
                            e.type = EventType.Updated;
                            break;
                        }
                    }
                    if (e.type == EventType.Unknown)
                    {
                        e.type = EventType.FileCreated;
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Debug.Print("File not found");
                e.type = EventType.FileCreated;
            }
            catch (Exception ex)
            {
                Debug.Print("Error in event handling: {0}", ex);
            }


            // determine type
            // add if relevant/not duplicate/...
            events.Add(e);
            Debug.Print("EVENT ADDED, type {0}", e.type.ToString());
        }
    }

    internal enum EventType
    {
        Unknown,
        Updated,
        FileCreated,
        FileDeleted,
        FileModified,
        FileMoved,
        FileRenamed,
        DirectoryCreated,
        DirectoryDeleted,
        DirectoryModified,
        DirectoryMoved,
        DirectoryRenamed
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
            this.eventManager = new EventManager();
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
