using System.Text.Json;
using System.Text.Json.Serialization;

namespace OnedataDrive.JSON_Object
{
    public class Config
    {
        public string onezone { get; set; } = "";
        public string provider_token { get; set; } = "";
        public string root_path { get; set; } = "";
        [JsonIgnore]
        public bool deleteExistingRootDir { get; set; } = false;
        [JsonIgnore]
        public bool enableRefresh { get; set; } = true;
        [JsonIgnore]
        public bool readOnly { get; set; } = true;

        public void Init(string path)
        {
            string json = File.ReadAllText(path);
            Config temp = JsonSerializer.Deserialize<Config>(json) ?? new();
            Init(temp.onezone, temp.root_path, temp.provider_token);
        }

        public void Init(string host, string path, string token)
        {
            onezone = host;
            provider_token = token;
            root_path = path;

            if (root_path.Length != 0 && root_path.Last() != '\\')
            {
                root_path += "\\";
            }
        }

        public bool IsComplete()
        {
            if (onezone == "" ||
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
}