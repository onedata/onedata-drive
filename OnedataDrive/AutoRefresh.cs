using System;
using System.Collections.Generic;
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

    static class AutoRefresh
    {
        private static List<string> monitored = new();
        public static IReadOnlyCollection<string> Monitored => monitored.AsReadOnly();
    }
}
