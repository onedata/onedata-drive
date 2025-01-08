using OnedataDrive.JSON_Object;
using System.Diagnostics;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive.Utils
{
    public static class PathUtils
    {
        public static string GetSpaceName(string fullPath)
        {
            string temp = fullPath.Replace(CloudSync.configuration.root_path, string.Empty);
            return temp.Split("\\")[0];
        }

        public static string GetParentPath(string fullPath, char separator = '\\')
        {
            string temp = fullPath;
            temp = temp.TrimEnd([separator]);
            temp = temp.Substring(0, temp.LastIndexOf(separator) + 1);
            return temp;
        }

        public static string GetLastInPath(string fullPath, char separator = '\\')
        {
            string temp = fullPath;
            temp = temp.TrimEnd([separator]);
            string[] arr = temp.Split(separator);
            return arr[arr.Length - 1];
        }

        /// <summary>
        /// NOTE: uses forward slash as separator in correctedPath and may contain backslash(\),
        /// but only as name character, not path separator
        /// </summary>
        /// <param name="fullPath">Path with backslash as separator</param>
        /// <returns></returns>
        public static string GetServerCorrectPath(string fullPath)
        {
            string path = fullPath;
            string pathFromSpace = GetPathFromSpace(fullPath);
            string correctedPath = "";
            List<ProviderInfo> providerInfos = CloudSync.spaces[GetSpaceName(path)].providerInfos;
            while (pathFromSpace.Length != 0 && pathFromSpace != "\\")
            {
                CF_PLACEHOLDER_BASIC_INFO info = CldApiUtils.GetBasicInfo(path);
                string fileId = System.Text.Encoding.Unicode.GetString(info.FileIdentity);
                var task = RestClient.GetFileAttribute(fileId, providerInfos);
                task.Wait();
                FileAttribute fa = task.Result;
                correctedPath = fa.name + "/" + correctedPath;
                path = GetParentPath(path);
                pathFromSpace = GetParentPath(pathFromSpace);
            }
            return correctedPath;
        }

        public static string GetPathFromSpace(string fullPath)
        {
            string path = fullPath.Substring(CloudSync.configuration.root_path.Length);
            if (path.Length > 0 && !path.EndsWith('\\'))
            {
                path += "\\";
            }
            return path;
        }
    }
}
