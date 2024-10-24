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
        Init(temp.zone_host, temp.root_path, temp.provider_token);
    }

    public void Init(string host, string path, string token)
    {
        zone_host = host;
        provider_token = token;
        root_path = path;

        if (root_path.Length != 0 && root_path.Last() != '\\')
        {
            root_path += "\\";
        }
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