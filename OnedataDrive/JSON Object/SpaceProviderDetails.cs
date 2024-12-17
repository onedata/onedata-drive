namespace OnedataDrive.JSON_Object
{
    public class SpaceProviderDetails
    {
        public string providerId { get; set; }
        public string name { get; set; }
        public string domain { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string clusterId { get; set; }
        public bool online { get; set; }
        public int creationTime { get; set; }

        public SpaceProviderDetails()
        {
            this.providerId = "";
            this.name = "";
            this.domain = "";
            this.latitude = 0;
            this.longitude = 0;
            this.clusterId = "";
            this.online = false;
            this.creationTime = 0;
        }
    }
}