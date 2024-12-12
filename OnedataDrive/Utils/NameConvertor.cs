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
        public const string PROHIBITED_CHARS = "<>:\"/\\|?*";
        char[] prohibited_chars = Path.GetInvalidFileNameChars();
        public bool WindowsCorrect(string name) 
        {
            return
                name.Length > 0
                && !name.Any(c => prohibited_chars.Contains(c))
                && !name.EndsWith(" ");
        }

        public string MakeWindowsCorrect(string name, char replaceChar = '_')
        {
            if (name.Length == 0)
            {
                return replaceChar.ToString();
            }

            StringBuilder sb = new StringBuilder();
            foreach (char c in name)
            {
                if (prohibited_chars.Any(pc => pc == c))
                {
                    sb.Append(replaceChar);
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (name.EndsWith(' '))
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
