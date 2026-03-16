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

    public class RootFolderAcessException : Exception
    {
        public RootFolderAcessException() : base() { }
        public RootFolderAcessException(string message) : base(message) { }
        public RootFolderAcessException(string message, Exception innerException) : base(message, innerException) { }

        public override string ToString()
        {
            return "RootFolderAcessException: " + base.ToString();
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

    public class NoSuchCloudFile : HttpRequestException
    {
        public NoSuchCloudFile(HttpRequestException hre) : base(hre.Message, hre.InnerException, hre.StatusCode) { }

        public NoSuchCloudFile(HttpRequestException hre, string message) : base(
                  hre.Message + "\n" + "NoSuchCloudFIle info: " + message, 
                  hre.InnerException, 
                  hre.StatusCode) { }

        public override string ToString()
        {
            return "NoSuchCloudFile: " + base.ToString();
        }
    }

    public class PlaceholderSizeException : Exception
    {
        public PlaceholderSizeException() : base() { }
        public PlaceholderSizeException(string message) : base(message) { }
        public PlaceholderSizeException(string message, Exception innerException) : base(message, innerException) { }
    }

    internal class EventExpiredException : Exception
    {
        public EventExpiredException(string message) : base(message) { }
        public EventExpiredException(string message, Exception inner) : base(message, inner) { }
    }

    public class NotPlaceholder : Exception
    {
        public NotPlaceholder() : base() { }
        public NotPlaceholder(string message) : base(message) { }
        public NotPlaceholder(string message, Exception innerException) : base(message, innerException) { }

        public override string ToString()
        {
            return "NotPlaceholder: " + base.ToString();
        }
    }

    public class InvalidFileEventException : Exception
    {
        public InvalidFileEventException() : base() { }
        public InvalidFileEventException(string message) : base(message) { }
        public InvalidFileEventException(string message, Exception innerException) : base(message, innerException) { }

        public override string ToString()
        {
            return "InvalidFileEventException: " + base.ToString();
        }
    }
}
