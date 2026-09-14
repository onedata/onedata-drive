namespace OnedataDrive
{
    public struct FileIdentity
    {
        public string fileID { get; set; }
        public string fileType { get; set; }

        public FileIdentity()
        {
            this.fileID = "";
            this.fileType = "";
        }

        public FileIdentity(string fileID, string fileType)
        {
            this.fileID = fileID;
            this.fileType = fileType;
        }
    }
}
