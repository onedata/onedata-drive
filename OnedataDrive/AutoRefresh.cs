using OnedataDrive.JSON_Object;
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
        EventType type;
        FileEvent fileEvent;
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

        public void AddEvent(FileEvent fileEvent)
        {
            Event e = new Event(fileEvent);
            events.Add(e);
            Debug.Print("EVENT ADDED");
            // determine type
            // add if relevant/not duplicate/...
        }
    }

    internal enum EventType
    {
        Unknown,
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
        private SpaceFolder spaceFolder;
        private List<string> monitored;
        private CancellationTokenSource cts;
        private EventManager eventManager;
        private Task monitoringTask;
        public IReadOnlyCollection<string> Monitored => monitored.AsReadOnly();
        public AutoRefresh(SpaceFolder spaceFolder)
        {
            this.monitored = new();
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

        public void AddToMonitored(string fileId)
        {
            if (!monitored.Contains(fileId))
            {
                monitored.Add(fileId);
                Debug.Print($"Added {fileId} to monitored list.");
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
            if (monitored.Count <= 0)
            {
                Debug.Print("No files to monitor. Exiting monitoring task.");
                return;
            }

            string spaceId = spaceFolder.spaceId;
            List<ProviderInfo> providerInfos = spaceFolder.providerInfos;
            Task<Stream> task = RestClient.GetFileEventStream(monitored, providerInfos, spaceId);
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
                            eventManager.AddEvent(fe);
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
