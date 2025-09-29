using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OnedataDrive.JSON_Object
{
    public class FileEvent
    {
        public string eventId { get; set; }
        public string eventType { get; set; }
        public string fileId { get; set; }
        public string parentFileId { get; set; }
        [JsonPropertyName("attributes")]
        public FileEventData data { get; set; }
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public FileEventData data_alias
        {
            get { return data; }
            set { data = value; }
        }

        public FileEvent()
        {
            eventId = string.Empty;
            eventType = string.Empty;
            fileId = string.Empty;
            parentFileId = string.Empty;
            data = new FileEventData();
        }

        public FileEvent(SseEvent sseEvent) : this()
        {
            FileEvent? fe = JsonSerializer.Deserialize<FileEvent>(sseEvent.data);
            if (fe != null)
            {
                eventId = sseEvent.eventId;
                eventType = sseEvent.eventType;
                fileId = fe.fileId;
                parentFileId = fe.parentFileId;
                data = fe.data;
            }
        }
    }

    public class FileEventData
    {
        public long index { get; set; }
        public string type { get; set; }
        public string activePermissionsType { get; set; }
        public string posixPermissions { get; set; }
        public string acl { get; set; }
        public string parentFileId { get; set; }
        public string originProviderId { get; set; }
        public List<string> directShareIds { get; set; }
        public string ownerUserId { get; set; }
        public long hardlinkCount { get; set; }
        public string symlinkValue { get; set; }
        public long creationTime { get; set; }
        public long atime { get; set; }
        public long mtime { get; set; }
        public long ctime { get; set; }
        public long size { get; set; }
        public bool isFullyReplicatedLocally { get; set; }
        public double localReplicationRate { get; set; }
        public bool hasCustomMetadata { get; set; }
        public bool hasJsonMetadata { get; set; }
        public string jsonMetadata { get; set; }
        public Dictionary<string, string> xattr { get; set; }
        public string name { get; set; }

        public FileEventData()
        {
            index = 0;
            type = string.Empty;
            activePermissionsType = string.Empty;
            posixPermissions = string.Empty;
            acl = string.Empty;
            parentFileId = string.Empty;
            originProviderId = string.Empty;
            directShareIds = new List<string>();
            ownerUserId = string.Empty;
            hardlinkCount = 0;
            symlinkValue = string.Empty;
            creationTime = 0;
            atime = 0;
            mtime = 0;
            ctime = 0;
            size = 0;
            isFullyReplicatedLocally = false;
            localReplicationRate = 0.0;
            hasCustomMetadata = false;
            hasJsonMetadata = false;
            jsonMetadata = string.Empty;
            xattr = new Dictionary<string, string>();
            name = string.Empty;
        }
    }

    public enum ObservedAttribute
    {
        [System.ComponentModel.Description("index")]
        index,
        [System.ComponentModel.Description("type")]
        type,
        [System.ComponentModel.Description("activePermissionsType")]
        activePermissionsType,
        [System.ComponentModel.Description("posixPermissions")]
        posixPermissions,
        [System.ComponentModel.Description("acl")]
        acl,
        [System.ComponentModel.Description("parentFileId")]
        parentFileId,
        [System.ComponentModel.Description("originProviderId")]
        originProviderId,
        [System.ComponentModel.Description("directShareIds")]
        directShareIds,
        [System.ComponentModel.Description("ownerUserId")]
        ownerUserId,
        [System.ComponentModel.Description("hardlinkCount")]
        hardlinkCount,
        [System.ComponentModel.Description("symlinkValue")]
        symlinkValue,
        [System.ComponentModel.Description("creationTime")]
        creationTime,
        [System.ComponentModel.Description("atime")]
        atime,
        [System.ComponentModel.Description("mtime")]
        mtime,
        [System.ComponentModel.Description("ctime")]
        ctime,
        [System.ComponentModel.Description("size")]
        size,
        [System.ComponentModel.Description("isFullyReplicatedLocally")]
        isFullyReplicatedLocally,
        [System.ComponentModel.Description("localReplicationRate")]
        localReplicationRate,
        [System.ComponentModel.Description("hasCustomMetadata")]
        hasCustomMetadata,
        [System.ComponentModel.Description("hasJsonMetadata")]
        hasJsonMetadata,
        [System.ComponentModel.Description("jsonMetadata")]
        jsonMetadata,
        [System.ComponentModel.Description("xattr")]
        xattr,
        [System.ComponentModel.Description("name")]
        name
    }

    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? value.ToString();
        }
    }
}
