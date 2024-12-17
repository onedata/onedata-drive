using System.Diagnostics;

namespace OnedataDrive.Utils
{
    public static class PathUtils
    {
        public static string GetSpaceName(string fullPath)
        {
            string temp = fullPath.Replace(CloudSync.configuration.root_path, string.Empty);
            return temp.Split("\\")[0];
        }

        public static string GetParentPath(string fullPath)
        {
            string temp = fullPath;
            temp = temp.TrimEnd(['\\']);
            temp = temp.Substring(0, temp.LastIndexOf("\\") + 1);
            return temp;
        }

        public static string GetLastInPath(string fullPath)
        {
            string temp = fullPath;
            temp = temp.TrimEnd(['\\']);
            string[] arr = temp.Split(@"\");
            return arr[arr.Length - 1];
        }
    }
}
