using OnedataDrive.Utils;
using static OnedataDrive.AutoRefresh;
using static Vanara.PInvoke.CldApi.CF_CALLBACK_PARAMETERS;

namespace OnedataDrive
{
    internal class ThreadSafeMonitored : ThreadSafeList<AutoRefresh.MonitoredFolder>
    {
        internal class DirectoryNotMonitoredException : Exception
        {
            public DirectoryNotMonitoredException(string message, Exception innerException) : base(message, innerException) { }
        }

        public bool AddToMonitored(string fileId, string path)
        {
            bool added = false;
            lock (_lock)
            {
                if (!_list.Any(x => x.id == fileId))
                {
                    _list.Add(new MonitoredFolder(fileId, path));

                    added = true;
                }
                return added;
            }
        }

        public bool RemoveFromMonitored(string fileId)
        {
            bool removed = true;
            lock (_lock)
            {
                MonitoredFolder? monitoredFolder = _list.FirstOrDefault(x => x.id == fileId);
                if (monitoredFolder != null && !_list.Remove(monitoredFolder))
                {
                    removed = !_list.Any(x => x.id == fileId);
                }
                return removed;
            }
        }

        public List<string> RenameMonitored(string fileId, string newName)
        {
            lock (_lock)
            {
                MonitoredFolder? monitoredFolder = _list.FirstOrDefault(x => x.id == fileId);
                if (monitoredFolder != null)
                {
                    string oldPath = monitoredFolder.path;
                    string newPath = PathUtils.ReplaceLastInPath(oldPath, newName);

                    return ReplaceInnerPath(oldPath, newPath);
                }
                return [];
            }
        }

        public List<string> RenameMonitoredPath(string fileId, string newPath)
        {
            lock (_lock)
            {
                MonitoredFolder? monitoredFolder = _list.FirstOrDefault(x => x.id == fileId);
                if (monitoredFolder != null)
                {
                    string oldPath = monitoredFolder.path;

                    return ReplaceInnerPath(oldPath, newPath);
                }
                return [];
            }
        }

        private List<string> ReplaceInnerPath(string oldPath, string newPath)
        {
            List<string> renamed = new();
            foreach (MonitoredFolder proccesedFolder in _list)
            {

                if (proccesedFolder.path.StartsWith(oldPath))
                {
                    string oldPathForLog = proccesedFolder.path;
                    string newPathForLog = proccesedFolder.path.Replace(oldPath, newPath);
                    proccesedFolder.path = newPathForLog;

                    renamed.Add($"oldPath: {oldPathForLog}, newPath: {newPathForLog}");
                }
            }
            return renamed;
        }
    }
}
