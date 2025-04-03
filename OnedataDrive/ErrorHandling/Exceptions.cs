namespace OnedataDrive.ErrorHandling
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException() : base() { }
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }

        public override string ToString()
        {
            return "ConfigurationException: " + base.ToString();
        }

    }

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

    public class InvalidTokenType : ProviderTokenException
    {
        public InvalidTokenType() : base() { }
        public InvalidTokenType(string message) : base(message) { }
        public InvalidTokenType(string message, Exception innerException) : base(message, innerException) { }

        public override string ToString()
        {
            return "InvalidTokenType: " + base.ToString();
        }
    }

    public class NoSuchCloudFile : Exception
    {
        public HttpResponseMessage? response = null;
        public NoSuchCloudFile() : base() { }
        public NoSuchCloudFile(string message) : base(message) { }
        public NoSuchCloudFile(string message, Exception innerException) : base(message, innerException) { }
        public NoSuchCloudFile(HttpResponseMessage rm) : base()
        {
            this.response = rm;
        }

        public override string ToString()
        {
            return "NoSuchCloudFile: " + base.ToString();
        }
    }

    public class RefreshStartConditions : Exception
    {
        public RefreshStartConditions() : base() { }
        public RefreshStartConditions(string message) : base(message) { }
        public RefreshStartConditions(string message, Exception innerException) : base(message, innerException) { }

        public override string ToString()
        {
            return "RefreshStartConditions: " + base.ToString();
        }
    }
}
