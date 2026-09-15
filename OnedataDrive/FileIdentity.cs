namespace OnedataDrive
{
    public struct FileIdentity
    {
        public string fileID { get; set; }
        public FileTypeOD fileType { get; set; }

        public FileIdentity()
        {
            this.fileID = "";
            this.fileType = FileTypeOD.EMPTY;
        }

        public FileIdentity(string fileID, FileTypeOD fileType)
        {
            this.fileID = fileID;
            this.fileType = fileType;
        }
    }
}
