public class SpaceFolder
{
    public string name;
    public string dirId;
    public string spaceId;
    //public string providerDomain;
    //public string providerId;
    public List<ProviderInfo> providerInfos;

    public SpaceFolder()
    {
        this.name = "";
        this.dirId = "";
        this.spaceId = "";
        //this.providerDomain = "";
        //this.providerId = "";
        this.providerInfos = new();
    }

    public SpaceFolder(string name, string dirId, string spaceId, ProviderInfo providerInfo)
    {
        this.name = name;
        this.dirId = dirId;
        this.spaceId = spaceId;
        this.providerInfos = [providerInfo];
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