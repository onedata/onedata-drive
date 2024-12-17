namespace OnedataDrive.JSON_Object
{
    public class Provider
    {
        public string providerId { get; set; }
        public string providerName { get; set; }

        public Provider()
        {
            this.providerId = "";
            this.providerName = "";
        }
    }

    public class SpaceDetails
    {
        public string name { get; set; }
        public string spaceId { get; set; }
        public string fileId { get; set; } // deprecated
        public string dirId { get; set; } // might be empty, fileId should be the same (not empty)
        public string trashDirId { get; set; }
        public string archivesDirId { get; set; }
        public List<Provider> providers { get; set; }

        public SpaceDetails()
        {
            this.name = "";
            this.spaceId = "";
            this.fileId = "";
            this.dirId = "";
            this.trashDirId = "";
            this.archivesDirId = "";
            this.providers = new();
        }
    }
}