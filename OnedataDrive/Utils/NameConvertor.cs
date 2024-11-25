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
    }
}
