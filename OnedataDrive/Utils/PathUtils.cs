using OnedataDrive.JSON_Object;
using System.Diagnostics;
using Windows.UI.Composition.Interactions;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive.Utils
{
    public static class PathUtils
    {
        public static string GetFullPath(string volumeDosName, string normalizedPath)
        {
            if (volumeDosName == string.Empty || normalizedPath == string.Empty)
            {
                throw new ArgumentException($"Empty parameter -> volumeDosName: {string.IsNullOrEmpty(volumeDosName)}," +
                    $"   normalizedPath: {string.IsNullOrEmpty(normalizedPath)}");
            }
            string fullPath = volumeDosName + normalizedPath;
            if (fullPath.Last() != '\\')
            {
                fullPath += '\\';
            }
            return fullPath;
        }

        public static string GetFullPath(CF_CALLBACK_INFO CallbackInfo)
        {
            return GetFullPath(CallbackInfo.VolumeDosName, CallbackInfo.NormalizedPath);
        }

        public static string GetFullPath(Callback callback)
        {
            return GetFullPath(callback.volumeDosName, callback.normalizedPath);
        }

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

        /// <summary>
        /// Checks if the given path is leading to a space folder, meaning that it starts with 
        /// root path and the first folder after root path is one of the space folders, 
        /// followed by either end of path or another folder/file.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static bool IsSpacePath(string fullPath)
        {
            if (fullPath.StartsWith(CloudSync.configuration.root_path))
            {
                string temp = fullPath.Replace(CloudSync.configuration.root_path, string.Empty);
                return CloudSync.spaces.Keys.Contains(temp.Split("\\")[0]);
            }
            return false;
        }

        /// <summary>
        /// Checks if the given path points exactly to the root folder
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static bool IsRootPath(string fullPath)
        {
            if (fullPath == string.Empty || fullPath.Last() != '\\')
            {
                fullPath += '\\';
            }
            return fullPath == CloudSync.configuration.root_path;
        }

        public static bool IsOnedataDrivePath(string fullPath)
        {
            return fullPath.StartsWith(CloudSync.configuration.root_path);
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

        public static string GetPlaceholderId(string placeholderPath)
        {
            CF_PLACEHOLDER_BASIC_INFO info = CldApiUtils.GetBasicInfo(placeholderPath);
            string id = System.Text.Encoding.Unicode.GetString(info.FileIdentity);
            return id;
        }

        public static SpaceFolder GetSpaceFolder(string fullPath)
        {
            string spaceName = GetSpaceName(fullPath);
            return CloudSync.spaces[spaceName];
        }

        public static string ReplaceLastInPath(string fullPath, string newLast, char separator = '\\')
        {
            if (newLast == string.Empty || fullPath == string.Empty)
            {
                return "";
            }
            string parentPath = GetParentPath(fullPath);
            string newPath = parentPath + newLast;
            if (newPath.Last() != separator)
            {
                newPath += separator;
            }
            return newPath;
        }
    }
}
