using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive.CloudSync.ErrorHandling
{
    public enum ReturnCodesEnum : uint
    {
        SUCCESS = 0,
        ERROR = 1,
        ROOT_FOLDER_NOT_EMPTY = 2
    }
}
