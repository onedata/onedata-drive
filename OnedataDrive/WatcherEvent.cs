using OnedataDrive.Interfaces;
using OnedataDrive.Utils;

namespace OnedataDrive
{
    public class WatcherEvent : IEvent<WatcherEvent>
    {
        public FileSystemEventArgs eventArgs;
        public object sender;
        public bool merged { get; private set; }
        public WatcherEvent(object sender, FileSystemEventArgs eventArgs)
        {
            this.sender = sender;
            this.eventArgs = eventArgs;
            this.merged = false;
        }

        public override void Merge(WatcherEvent mergeWith)
        {
            if (this.eventArgs.FullPath != mergeWith.eventArgs.FullPath)
            {
                throw new ArgumentException("Cannot merge UploadRequests with different file paths!");
            }
            WatcherChangeTypes changeTypes = this.eventArgs.ChangeType | mergeWith.eventArgs.ChangeType;

            string? name = null;
            string path = this.eventArgs.FullPath;
            if (!string.IsNullOrEmpty(this.eventArgs.Name))
            {
                name = this.eventArgs.Name;
                path = this.eventArgs.FullPath[..^name.Length];
            }
            this.eventArgs = new(changeTypes, path, name);

            this.merged = true;
        }

        public override string RelationKey()
        {
            return eventArgs.FullPath;
        }

        public override string ToString()
        {
            return $"[WatcherEvent: ChangeType={eventArgs.ChangeType}, FullPath={eventArgs.FullPath}, Name={eventArgs.Name}], Merged={merged}, EventID={AEventId}";
        }
    }
}
