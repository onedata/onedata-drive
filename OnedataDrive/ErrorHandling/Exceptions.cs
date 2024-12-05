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

    public class OnezoneException : Exception
    {
        public OnezoneException() : base() { }
        public OnezoneException(string message) : base(message) { }
        public OnezoneException(string message, Exception innerException) : base(message, innerException) { }

        public override string ToString()
        {
            return "OnezoneException: " + base.ToString();
        }
    }

    public class ProviderTokenException : Exception
    {
        public ProviderTokenException() : base() { }
        public ProviderTokenException(string message) : base(message) { }
        public ProviderTokenException(string message, Exception innerException) : base(message, innerException) { }

        public override string ToString()
        {
            return "ProviderTokenException: " + base.ToString();
        }
    }

}
