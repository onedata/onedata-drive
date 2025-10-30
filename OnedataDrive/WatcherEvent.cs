using OnedataDrive.Interfaces;
using OnedataDrive.Utils;

namespace OnedataDrive
{
    public class WatcherEvent : IEvent<WatcherEvent>
    {
        FileSystemEventArgs eventArgs;
        public bool merged { get; private set; }
        public WatcherEvent(FileSystemEventArgs eventArgs)
        {
            this.eventArgs = eventArgs;
            this.merged = false;
        }

        public void Merge(WatcherEvent mergeWith)
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
                path = PathUtils.GetParentPath(path);
            }
            this.eventArgs = new(changeTypes, this.eventArgs.FullPath, null);

            this.merged = true;
        }

        public string RelationKey()
        {
            return eventArgs.FullPath;
        }
    }
}
