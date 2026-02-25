namespace OnedataDrive.JSON_Object
{
    public class Child
    {
        public string fileId { get; set; }
        public string index { get; set; }
        public string type { get; set; }
        public string activePermissionsType { get; set; }
        public long posixPermissions { get; set; }
        public List<Acl> acl { get; set; }
        public string name { get; set; }
        public string conflictingName { get; set; }
        public string path { get; set; }
        public string parentFileId { get; set; }
        public long displayGid { get; set; }
        public long displayUid { get; set; }
        public long creationTime { get; set; }
        public long atime { get; set; }
        public long mtime { get; set; }
        public long ctime { get; set; }
        public long size { get; set; }
        public bool isFullyReplicatedLocally { get; set; }
        public double localReplicationRate { get; set; }
        public string originProviderId { get; set; }
        public List<string> directShareIds { get; set; }
        public string ownerUserId { get; set; }
        public long hardlinkCount { get; set; }
        public string symlinkValue { get; set; }
        public List<string> effProtectionFlags { get; set; }
        public List<string> effDatasetProtectionFlags { get; set; }
        public string effDatasetInheritancePath { get; set; }
        public string effQosInheritancePath { get; set; }
        public string aggregateQosStatus { get; set; }
        public string archiveRecallRootFileId { get; set; }
        public bool hasCustomMetadata { get; set; }
        public bool hasJsonMetadata { get; set; }
        //public object jsonMetadata { get; set; }
        //public Dictionary<string, string> xattr { get; set; }



        

        public Child()
        {
            this.fileId = "";
            this.index = "";
            this.type = "";
            this.activePermissionsType = "";
            this.acl = new();
            this.name = "";
            this.conflictingName = "";
            this.path = "";
            this.parentFileId = "";
            this.originProviderId = "";
            this.directShareIds = new();
            this.ownerUserId = "";
            this.symlinkValue = "";
            this.effProtectionFlags = new();
            this.effDatasetProtectionFlags = new();
            this.effDatasetInheritancePath = "";
            this.effQosInheritancePath = "";
            this.aggregateQosStatus = "";
            this.archiveRecallRootFileId = "";
        }
    }

    public class Acl
    {
        public string aceType { get; set; }
        public string identifier { get; set; }
        public int aceFlags { get; set; }
        public int acemask { get; set; }

        public static readonly string ACE_TYPE_ALLOW = "ALLOW";
        public static readonly string ACE_TYPE_DENY = "DENY";

        public Acl()
        {
            this.aceType = "";
            this.identifier = "";
        }
    }

    public class DirChildren
    {
        public List<Child> children { get; set; }
        public bool isLast { get; set; }
        public string nextPageToken { get; set; }

        public DirChildren()
        {
            this.children = new();
            this.nextPageToken = "";
        }
    }
}