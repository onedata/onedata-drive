using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive.JSON_Object
{
    public class FileEvent
    {
        public string eventId { get; set; }
        public string eventType { get; set; }
        public string fileId { get; set; }
        public string parentFileId { get; set; }
        public FileEventData data { get; set; }

        public FileEvent()
        {
            eventId = string.Empty;
            eventType = string.Empty;
            fileId = string.Empty;
            parentFileId = string.Empty;
            data = new FileEventData();
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
        }
    }
}
