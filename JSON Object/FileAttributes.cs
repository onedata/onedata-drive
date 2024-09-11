public class FileAttribute
    {
        public string name { get; set; } = "";
        public string type { get; set; } = "";
        public string mode { get; set; } = "";
        public long size { get; set; }
        public long atime { get; set; }
        public long mtime { get; set; }
        public long ctime { get; set; }
        public string owner_id { get; set; } = "";
        public string file_id { get; set; } = "";
        public string parent_id { get; set; } = "";
        public string provider_id { get; set; } = "";
        public int storage_user_id { get; set; }
        public int storage_group_id { get; set; }
        public List<string> shares { get; set; } = new();
        public int hardlinks_count { get; set; }
        public string index { get; set; } = "";
    }