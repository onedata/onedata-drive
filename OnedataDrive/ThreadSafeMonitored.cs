using NLog;
using OnedataDrive.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Text.Core;
using static OnedataDrive.AutoRefresh;

namespace OnedataDrive
{
    internal class ThreadSafeMonitored : ThreadSafeList<AutoRefresh.MonitoredFolder>
    {
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
