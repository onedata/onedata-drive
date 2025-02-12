using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive.Utils
{
    public enum Commands
    {
        SEND_ROOT,
        SELECTED_PATHS,
        RECEIVED,
        FAIL
    }
    public static class NamedPipeUtils
    {
        public static string CreateCommandMsg(Commands command, string[]? content = null)
        {
            content = content ?? Array.Empty<string>();
            string msg = Commands.SELECTED_PATHS.ToString();
            if (content.Length != 0)
            {
                msg += "|" + string.Join("|", content);
            }
            return msg;
        }
    }
}
