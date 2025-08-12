namespace OnedataDrive.JSON_Object
{
    public class TokenExamine
    {
        public AccessToken accessToken { get; set; } = new();
        public Subject subject { get; set; } = new();
        public string persistance { get; set; } = "";
        public string onezoneDomain { get; set; } = "";
        public string id { get; set; } = "";
        public List<Caveat> caveats { get; set; } = new();

        public bool isRestInterface()
        {
            return caveats.Any(caveat => caveat.type == "interface" && caveat.@interface == "rest");
        }
    }

    public class AccessToken
    {
    }

    public class Subject
    {
        public string type { get; set; } = "";
        public string id { get; set; } = "";
    }

    public class Caveat
    {
        public string type { get; set; } = "";
        public string @interface { get; set; } = "";
    }
}
