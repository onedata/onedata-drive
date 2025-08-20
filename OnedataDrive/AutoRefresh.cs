using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive
{
    public class Event
    {
        int eventId;
        EventType type;

        // timestamp
        // event info
    }

    class EventManager
    {
        string fileId;
        string path;
        List<Event> events;

        public EventManager()
                    {
            fileId = string.Empty;
            path = string.Empty;
            events = new List<Event>();
        }
    }

    internal enum EventType
    {
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

    public static class AutoRefresh
    {
        private static List<string> monitored = new();
        public static IReadOnlyCollection<string> Monitored => monitored.AsReadOnly();
        public static void AddToMonitored(string fileId)
        {
            if (!monitored.Contains(fileId))
            {
                monitored.Add(fileId);
                Debug.Print($"Added {fileId} to monitored list.");
            }
            else
            {
                Debug.Print($"{fileId} is already in the monitored list.");
            }
        }

        public static void TestMethod(CancellationToken token)
        {
            //List<string> dirId = ["0000000000525D3D67756964233162376462346636383039646435613264626333626334373239653434336666636862616465236235303662623335653933336539656531366361633830366336616436346431636836386366"];
            string spaceId = "b506bb35e933e9ee16cac806c6ad64d1ch68cf";
            SpaceFolder sf = CloudSync.spaces["testAG_e-INFRA"];
            var task = RestClient.GetFileEventStream(monitored, sf.providerInfos, spaceId);
            task.Wait();
            using (Stream stream = task.Result)
            {
                using (StreamReader reader = new(stream))
                {
                    while (true)
                    {  
                        try
                        {
                            Task<string?> readTask = reader.ReadLineAsync(token).AsTask();
                            int time = 0;
                            while (!readTask.IsCompleted)
                            {
                                Debug.Print($"Waiting for read: {time}s");
                                Thread.Sleep(1000);
                                time++;
                            }
                            string line = readTask.Result ?? "NOTHING WAS READ";
                            Debug.Print($"READ LINE: {line}");
                        }
                        catch (Exception e) when (e is OperationCanceledException || e is ObjectDisposedException)
                        {
                            Debug.Print("Read operation was cancelled: {0}", e);
                        }
                        catch (Exception e)
                        {
                            Debug.Print("Error reading line: {0}", e.Message);
                        }

                        if (token.IsCancellationRequested)
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
