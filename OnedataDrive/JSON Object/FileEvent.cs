using OnedataDrive.Interfaces;
using System;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OnedataDrive.JSON_Object
{
    public class FileEvent : IEvent<FileEvent>
    {
        [JsonIgnore]
        public const string EVENT_CHANGED = "changedOrCreated";
        [JsonIgnore]
        public const string EVENT_DELETED = "deleted";
        [JsonIgnore]
        public bool isMerged { get; private set; }
        public string SSEventId { get; set; }
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
            isMerged = false;
            SSEventId = string.Empty;
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
                SSEventId = sseEvent.SSEventId;
                eventType = sseEvent.eventType;
                fileId = fe.fileId;
                parentFileId = fe.parentFileId;
                data = fe.data;
            }
        }

        /// <summary>
        /// Merges properties: SSEventId, eventType, data.size, data.mtime, data.name, data.type
        /// </summary>
        /// <param name="newEvent"></param>
        /// <exception cref="ArgumentException"></exception>
        public override void Merge(FileEvent newEvent)
        {
            if (this.fileId != newEvent.fileId)
            {
                throw new ArgumentException("Cannot update FileEvent with different fileId");
            }

            SSEventId = newEvent.SSEventId;
            if (!string.IsNullOrWhiteSpace(newEvent.eventType) && newEvent.eventType == EVENT_DELETED) this.eventType = newEvent.eventType;
            if (newEvent.data.size != null) data.size = newEvent.data.size;
            if (newEvent.data.mtime != null) data.mtime = newEvent.data.mtime;
            if (!string.IsNullOrWhiteSpace(newEvent.data.name)) data.name = newEvent.data.name;
            if (!string.IsNullOrWhiteSpace(newEvent.data.type)) data.type = newEvent.data.type;

            isMerged = true;
        }

        public override string RelationKey()
        {
            return fileId;
        }

        public override string ToString()
        {
            List<string> list = MoreInfo();
            return $"[{string.Join("| ", list)}]";
        }

        public override List<string> MoreInfo()
        {
            List<string> list = new()
            {
                "FileEvent: ",
                $"SSEventId={SSEventId}",
                $"EventType={eventType}",
                $"Name={data.name}",
                $"Merged={isMerged}",
                $"Size={data.size}",
                $"FileId/RelationKey={fileId}",
                $"ParentId={parentFileId}"
            };
            return list;
        }
    }

    public class FileEventData
    {
        public long? index { get; set; }
        public string? type { get; set; }
        public string? activePermissionsType { get; set; }
        public string? posixPermissions { get; set; }
        public string? acl { get; set; }
        public string? parentFileId { get; set; }
        public string? originProviderId { get; set; }
        public List<string>? directShareIds { get; set; }
        public string? ownerUserId { get; set; }
        public long? hardlinkCount { get; set; }
        public string? symlinkValue { get; set; }
        public long? creationTime { get; set; }
        public long? atime { get; set; }
        public long? mtime { get; set; }
        public long? ctime { get; set; }
        public long? size { get; set; }
        public bool? isFullyReplicatedLocally { get; set; }
        public double? localReplicationRate { get; set; }
        public bool? hasCustomMetadata { get; set; }
        public bool? hasJsonMetadata { get; set; }
        public string? jsonMetadata { get; set; }
        public Dictionary<string, string>? xattr { get; set; }
        public string? name { get; set; }

        public FileEventData()
        {

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
