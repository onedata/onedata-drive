using System.Text.Json;

public class Config
{
    public string zone_host { get; set; } = "";
    public string provider_token { get; set; } = "";
    public string root_path { get; set; } = "";

    public void Init(string path)
    {
        string json = File.ReadAllText(path);
        Config temp = JsonSerializer.Deserialize<Config>(json) ?? new();
        zone_host = temp.zone_host;
        provider_token = temp.provider_token;
        root_path = temp.root_path;
    }

    public bool IsComplete()
    {
        if (zone_host == "" ||
            provider_token == "" ||
            root_path == "" ||
            root_path.Last() != '\\'
            )
        {
            return false;
        }
        return true;
    }
}