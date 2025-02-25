using System.Text;

namespace OnedataDrive.Utils
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

        /// <summary>
        /// If <paramref name="name"/> is not correct Windows file name, it will be renamed: 
        /// invalid character will be replaced with <paramref name="replaceChar"/>. In case of prohibited name in format <c>PROHIBITED.ANY_TEXT</c> 
        /// <paramref name="replaceChar"/> will be attached as prefix.
        /// If <paramref name="name"/> was incorrect Windows name, <c>@</c> will be appended folowed by 10 characters from <paramref name="fileId"/> starting at index 25. 
        /// If <paramref name="fileId"/> is not long enough only <c>@</c> will be appended.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="modified">Output parameter. <c>true</c> if name was modified and ID attached. <c>false</c> otherwise</param>
        /// <param name="replaceChar"></param>
        /// <param name="fileId"></param>
        /// <returns>Correct Windows name</returns>
        public string MakeWindowsCorrect(string name, out bool modified, string fileId, char replaceChar = '_')
        {

            string? fixedName = FixName(name, replaceChar);
            if (fixedName == null)
            {
                modified = false;
                return name;
            }
            else
            {
                modified = true;
                return fixedName + IdSuffix(fileId);
            }
        }

        public string MakeWindowsCorrectDistinct(string name, string fileId, PlaceholderCreateInfo createInfo, char replaceChar = '_')
        {
            bool idAttached;
            string newName = MakeWindowsCorrect(name, out idAttached, fileId);

            if (!idAttached && createInfo.Get().Any(x => x.RelativeFileName.ToLower() == (newName).ToLower()))
            {
                newName = newName + IdSuffix(fileId);
            }

            string suffix = "";
            for (int i = 2; createInfo.Get().Any(x => x.RelativeFileName.ToLower() == (newName + suffix).ToLower()); i++)
            {
                suffix = "(" + i.ToString() + ")";
            }
            return newName + suffix;
        }

        private string? FixName(string name, char replaceChar = '_')
        {
            // name is empty
            if (name.Length == 0)
            {
                return replaceChar.ToString();
            }

            // name is prohibited or prohibited followed by .
            string firstStageCorrected = name;
            foreach (string prohibited in prohibitedNames)
            {
                if (name == prohibited)
                {
                    return name; 
                }

                if (name.StartsWith(prohibited + "."))
                {
                    firstStageCorrected = replaceChar.ToString() + name;
                    break;
                }
            }

            // change prohibited characters
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

            // if name was correct return null
            if (sb.ToString() == name)
            {
                return null;
            }
            else
            {
                return sb.ToString();
            }
        }

        public static string IdSuffix(string fileId)
        {
            if (fileId.Length >= 36)
            {
                return "@" + fileId.Substring(25, 10);
            }
            else
            {
                return "@";
            }
        }
    }
}
