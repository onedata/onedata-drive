using OnedataDrive.Utils;
using static OnedataDrive.AutoRefresh;

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

        public List<string> RenameMonitored(string fileId, string newName)
        {
            lock (_lock)
            {
                List<string> renamed = new();
                MonitoredFolder? monitoredFolder = _list.FirstOrDefault(x => x.id == fileId);
                if (monitoredFolder != null)
                {
                    string oldPath = monitoredFolder.path;
                    string newPath = PathUtils.ReplaceLastInPath(oldPath, newName);

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
                }
                return renamed;
            }
        }
    }
}
