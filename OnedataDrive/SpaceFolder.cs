namespace OnedataDrive
{
    public class SpaceFolder
    {
        public string name;
        public string dirId;
        public string spaceId;
        public List<ProviderInfo> providerInfos;
        public AutoRefresh? autoRefresh;

        public SpaceFolder()
        {
            this.name = "";
            this.dirId = "";
            this.spaceId = "";
            this.providerInfos = new();
        }

        public SpaceFolder(string name, string dirId, string spaceId, ProviderInfo providerInfo, bool autorefresh = true)
        {
            this.name = name;
            this.dirId = dirId;
            this.spaceId = spaceId;
            this.providerInfos = [providerInfo];
            if (autorefresh)
            {
                this.autoRefresh = new AutoRefresh(this);
            }
        }
    }

    public class ProviderInfo
    {
        public string providerId = "";
        public string providerDomain = "";

        public ProviderInfo(string id, string domain)
        {
            providerId = id;
            providerDomain = domain;
        }

        public override string ToString()
        {
            return providerDomain;
        }
    }
}