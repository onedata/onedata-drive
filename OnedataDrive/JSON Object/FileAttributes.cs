namespace OnedataDrive.JSON_Object
{
    using System.Text.Json.Serialization;

    public class FileAttribute
    {
        public string name { get; set; } = "";

        [JsonConverter(typeof(FileTypeODConverter))]
        [JsonPropertyName("type")]
        public FileTypeOD fileType { get; set; } = FileTypeOD.EMPTY;
        public string posixPermissions { get; set; } = "";
        public long size { get; set; }
        public long atime { get; set; }
        public long mtime { get; set; }
        public long ctime { get; set; }
        public string ownerUserId { get; set; } = "";
        public string fileId { get; set; } = "";
        public string parentFileId { get; set; } = "";
        public string originProviderId { get; set; } = "";
        public int displayUid { get; set; }
        public int displayGid { get; set; }
        public List<string> directShareIds { get; set; } = new();
        public int hardlinkCount { get; set; }
        public string index { get; set; } = "";
        public string symlinkValue { get; set; } = "";
        public bool isFullyReplicatedLocally { get; set; }
    }
}