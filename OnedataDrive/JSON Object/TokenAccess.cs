namespace OnedataDrive.JSON_Object
{
    public class TokenAccess
    {
        public int? validUntil { get; set; }
        public DataAccessScope dataAccessScope { get; set; } = new();
    }

    public class DataAccessScope
    {
        public bool @readonly { get; set; }
        public Dictionary<string, TASpace> spaces { get; set; } = new(); // KEY is spaceId
        public Dictionary<string, ProviderData> providers { get; set; } = new(); // KEY is providerId
    }

    public class TASpace
    {
        public string name { get; set; } = "";
        public Dictionary<string, Support> supports { get; set; } = new(); // KEY is providerId
    }

    public class Support
    {
        public bool @readonly { get; set; }
    }

    public class ProviderData
    {
        public string version { get; set; } = "";
        public bool online { get; set; }
        public string name { get; set; } = "";
        public string domain { get; set; } = "";
    }
}