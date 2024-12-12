using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace OnedataDrive.CloudSync.Utils
{
    public class NameConvertor
    {
        //public const string PROHIBITED_CHARS = "<>:\"/\\|?*";
        char[] prohibitedChars = Path.GetInvalidFileNameChars();
        List<string> prohibitedNames = new() {
            "CON", "PRN", "AUX", "NUL", "COM0", "COM1", "COM2", 
            "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", 
            "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", 
            "LPT7", "LPT8", "LPT9"
        };
        public bool WindowsCorrect(string name) 
        {
            foreach (string prohibited in prohibitedNames)
            {
                if (name == prohibited || name.StartsWith(prohibited + "."))
                {
                    return false;
                }
            }

            return
                name.Length > 0
                && !name.Any(c => prohibitedChars.Contains(c))
                && !name.EndsWith(" ");
        }

        public string MakeWindowsCorrect(string name, char replaceChar = '_')
        {
            if (name.Length == 0)
            {
                return replaceChar.ToString();
            }

            string firstStageCorrected = name;

            foreach (string prohibited in prohibitedNames)
            {
                if (name == prohibited || name.StartsWith(prohibited + "."))
                {
                    firstStageCorrected = replaceChar.ToString() + name;
                    break;
                }
            }

            StringBuilder sb = new StringBuilder();
            foreach (char c in firstStageCorrected)
            {
                if (prohibitedChars.Any(pc => pc == c))
                {
                    sb.Append(replaceChar);
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (firstStageCorrected.EndsWith(' '))
            {
                sb.Remove(sb.Length - 1, 1);
            }
            return sb.ToString();
        }

        public bool MakeWindowsCorrect(string name, out string newName, char replaceChar = '_')
        {
            newName = MakeWindowsCorrect(name, replaceChar);
            if (newName == name)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// If <paramref name="name"/> is not correct Windows file name, it will be renamed: 
        /// invalid character will be replaced with <paramref name="replaceChar"/>. Modified name will be placed in output parameter <paramref name="newName"/>.
        /// If <paramref name="name"/> was modified <c>@</c> will be appended folowed by 10 characters from <paramref name="fileId"/> starting at index 25. 
        /// If <paramref name="fileId"/> is not long enough only <c>@</c> will be appended.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newName">Output parameter. Contains modified name. If name was not changed contains original name.</param>
        /// <param name="replaceChar"></param>
        /// <param name="fileId"></param>
        /// <returns><c>true</c> if name was already correct. <c>false</c> otherwise</returns>
        public bool MakeWindowsCorrect(string name, out string newName, string fileId, char replaceChar = '_')
        {
            newName = MakeWindowsCorrect(name, replaceChar);
            if (newName == name)
            {
                return true;
            }
            else
            {
                newName += "@";
                if (fileId.Length >= 36)
                {
                    newName += fileId.Substring(25, 10);
                }
                return false;
            }
        }
    }
}
