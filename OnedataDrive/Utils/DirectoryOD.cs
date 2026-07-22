using System;
using System.Collections.Generic;
using System.Text;

namespace OnedataDrive.Utils
{
    public static class DirectoryOD
    {
        public static void Delete(Callback callback)
        {
            string fullPath = PathUtils.GetFullPath(callback);
            string fileIdentity = callback.fileIdentity;
            Delete(fullPath, fileIdentity);
        }

        public static void Delete(string fullPath, string fileIdentity)
        {
            Directory.Delete(fullPath, recursive: true);
            PathUtils.GetSpaceFolder(fullPath).autoRefresh?.RemoveFromMonitored(fileIdentity);
        }
    }
}
