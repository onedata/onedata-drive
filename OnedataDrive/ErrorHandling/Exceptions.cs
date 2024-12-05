using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive.CloudSync.Exceptions
{
    public class RootFolderNotEmptyException : Exception
    {
        public RootFolderNotEmptyException() : base() { }
        public RootFolderNotEmptyException(string message) : base(message) { }
        public RootFolderNotEmptyException(string message, Exception innerException) : base(message, innerException) { }

        public override string ToString()
        {
            return "RootFolderNotEmptyException: " + base.ToString();
        }

    }

    public class JsonReturnedNullException : Exception
    {
        public JsonReturnedNullException() : base() { }
        public JsonReturnedNullException(string message) : base(message) { }
        public JsonReturnedNullException(string message, Exception innerException) : base(message, innerException) { }

        public override string ToString()
        {
            return "JsonReturnedNullException: " + base.ToString();
        }
    }
    
}
