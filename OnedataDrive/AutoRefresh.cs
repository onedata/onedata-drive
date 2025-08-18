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

        public static void TestMethod(CancellationToken token)
        {
            List<string> dirId = ["000000000052F6FC67756964233434346362383330613136656161383064393361366132383833613764343631636831343562236235303662623335653933336539656531366361633830366336616436346431636836386366"];
            string spaceId = "b506bb35e933e9ee16cac806c6ad64d1ch68cf";
            SpaceFolder sf = CloudSync.spaces["testAG_e-INFRA"];
            var task = RestClient.GetFileEventStream(dirId, sf.providerInfos, spaceId);
            task.Wait();
            using (Stream stream = task.Result)
            {
                using (StreamReader reader = new(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line == null) continue;
                        // Process the line
                        Debug.Print("\nFileEvent: " + line);
                        Debug.Print("\n\n");
                        if (token.IsCancellationRequested)
                        {
                            Debug.Print("Cancellation requested, stopping processing.");
                            break;
                        }
                        // Here you can parse the line as needed, e.g., deserialize JSON
                        // var fileEvent = JsonSerializer.Deserialize<FileEvent>(line);
                        // Do something with fileEvent
                    }
                }
            }
        }
    }
}
