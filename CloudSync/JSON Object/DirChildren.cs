public class Child
    {
        public string name { get; set; }
        public string type { get; set; }
        public int mode { get; set; }
        public long size { get; set; }
        public long atime { get; set; }
        public long mtime { get; set; }
        public long ctime { get; set; }
        public string owner_id { get; set; }
        public string file_id { get; set; }
        public string parent_id { get; set; }
        public string provider_id { get; set; }
        public int storage_user_id { get; set; }
        public int storage_group_id { get; set; }
        public List<string> shares { get; set; }
        public int hardlinks_count { get; set; }

        public Child()
        {
            this.name = "";
            this.type = "";
            this.owner_id = "";
            this.file_id = "";
            this.parent_id = "";
            this.provider_id = "";
            this.shares = new();
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