using OnedataDrive.CloudSync.Exceptions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

static class RestClient
{
    public static string PROVIDER_TOKEN = "";
    public static string ZONE_HOST = "";
    public static string ZONE_PROTOCOL = "";
    private static HttpClient client = new();
    private static HttpClient clientNoHeaders = new();
    private const string HTTP = "http://";
    private const string HTTPS = "https://";

    public static void Init(Config config)
    {
        PROVIDER_TOKEN = config.provider_token;
        if (config.zone_host.StartsWith(HTTPS))
        {
            ZONE_HOST = config.zone_host.Substring(8);
            ZONE_PROTOCOL = HTTPS;
        }
        else if (config.zone_host.StartsWith(HTTP))
        {
            ZONE_HOST = config.zone_host.Substring(7);
            ZONE_PROTOCOL = HTTP;
        }
        else
        {
            ZONE_HOST = config.zone_host;
            ZONE_PROTOCOL = HTTPS;
        }

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("x-auth-token", PROVIDER_TOKEN);

        clientNoHeaders.DefaultRequestHeaders.Clear();
    }
    private static async Task<T> OnedataGet<T>(string url)
    {
        var response = await client.GetAsync(url);

        response.EnsureSuccessStatusCode();

        T? data = JsonSerializer.Deserialize<T>(response.Content.ReadAsStream());
        response.Dispose();
        return data ??
         throw new Exception("Failed to deserialize JSON. URL: " + url);
    }

    private static async Task<string> OnedataGetString(string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
    
    private static async Task<byte[]> OnedataGetByteArr(string url)
    {
        var response = await client.GetAsync(url);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync();
    }

    private static async Task<Stream> OnedataGetStream(string url)
    {
        Stream response = await client.GetStreamAsync(url);
        return response;
    }

    private static async Task OnedataDelete(string url)
    {
        var response = await client.DeleteAsync(url);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var task = response.Content.ReadAsStringAsync();
            task.Wait();
            string responseText = task.Result; 
            if (responseText.Contains("\"errno\":\"enoent\""))
            {
                return;
            }
        }

        response.EnsureSuccessStatusCode();

        return;
    }

    private static async Task<T> OnedataPost<T>(string url, HttpContent? content)
    {
        HttpRequestMessage RequestMsg = new(HttpMethod.Post, url)
        {
            Content = content
        };

        var response = await client.SendAsync(RequestMsg);

        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<T>(response.Content.ReadAsStream()) ??
         throw new Exception("Failed to deserialize JSON.");
    }

    private static async Task OnedataPut(string url, HttpContent content)
    {
        HttpRequestMessage RequestMsg = new(HttpMethod.Put, url)
        {
            Content = content
        };

        var response = await client.SendAsync(RequestMsg);
        response.EnsureSuccessStatusCode();

        return;
    }

    public static async Task<TokenAccess> InferAccessTokenScope()
    { 
        string url = ZONE_PROTOCOL
            + ZONE_HOST 
            + "/api/v3/onezone/tokens/infer_access_token_scope";
        
        string json = "{\"token\":\"" + PROVIDER_TOKEN + "\"}";
        StringContent content = new(json);
        content.Headers.Clear();
        content.Headers.Add("Content-Type", "application/json");

        var response = await clientNoHeaders.PostAsync(url, content);

        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<TokenAccess>(response.Content.ReadAsStream()) ??
         throw new JsonReturnedNullException();
    }

    public static async Task<SpaceDetails> GetSpacesDetails(string spaceId, string provider_domain)
    { 
        string url = "https://" + provider_domain + "/api/v3/oneprovider/spaces/" + spaceId;
        return await OnedataGet<SpaceDetails>(url);
    }

    public static async Task<DirChildren> GetFilesAndSubdirs(string dirId, string providerDomain)
    { 
        string url = "https://" 
            + providerDomain 
            + "/api/v3/oneprovider/data/" 
            + dirId 
            + "/children?attribute=size&attribute=name&attribute=type&attribute=atime&attribute=mtime&attribute=ctime&attribute=file_id&limit=1000";
        return await OnedataGet<DirChildren>(url);
    }

    public static async Task<DirChildren> GetFilesAndSubdirs(string dirId, List<ProviderInfo> providerInfos)
    { 
        foreach (ProviderInfo info in providerInfos)
        {
            try
            {
                string url = "https://" 
                    + info.providerDomain 
                    + "/api/v3/oneprovider/data/" 
                    + dirId 
                    + "/children?attribute=size&attribute=name&attribute=type&attribute=atime&attribute=mtime&attribute=ctime&attribute=file_id&limit=1000";
                return await OnedataGet<DirChildren>(url);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        throw new Exception("Failed to get response");
    }

    public static async Task<byte[]> GetData(string provider_domain, string fileId)
    { 
        string url = "https://" + provider_domain + "/api/v3/oneprovider/data/" + fileId + "/content";
        return await OnedataGetByteArr(url);
    }

    public static async Task<byte[]> GetData(List<ProviderInfo> providerInfos, string fileId)
    { 
        foreach (ProviderInfo info in providerInfos)
        {
            try
            {
                string url = "https://" + info.providerDomain + "/api/v3/oneprovider/data/" + fileId + "/content";
                return await OnedataGetByteArr(url);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        throw new Exception("Failed to get response");
    }

    public static async Task<Stream> GetStream(List<ProviderInfo> providerInfos, string fileId)
    { 
        foreach (ProviderInfo info in providerInfos)
        {
            try
            {
                string url = "https://" + info.providerDomain + "/api/v3/oneprovider/data/" + fileId + "/content";
                return await OnedataGetStream(url);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        throw new Exception("Failed to get response");
    }

    public static async Task Delete(List<ProviderInfo> providerInfos, string fileId)
    { 
        foreach (ProviderInfo info in providerInfos)
        {
            try
            {
                string url = "https://" + info.providerDomain + "/api/v3/oneprovider/data/" + fileId;
                await OnedataDelete(url);
                return;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        throw new Exception("Failed to get response");
    }

    public static async Task<FileId> CreateFileInDir(List<ProviderInfo> providerInfos, string parentId, string name, FileStream? stream = null, bool directory = false)
    { 
        foreach (ProviderInfo info in providerInfos)
        {
            string url = "https://" + info.providerDomain + ":443/api/v3/oneprovider/data/" + parentId + "/children?name=" + name;
            try
            {
                if (directory)
                {
                    string urlDir = url + "&type=DIR";
                    return await OnedataPost<FileId>(urlDir, null);
                }
                else
                {
                    StreamContent? content = null;
                    if (stream != null)
                    {
                        content = new StreamContent(stream);
                        content.Headers.Add("Content-Type", "application/octet-stream");
                    }
                    return await OnedataPost<FileId>(url, content);
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        throw new Exception("Failed to post file");
    }

    public static async Task PostFileContent(List<ProviderInfo> providerInfos, string id, FileStream stream)
    { 
        foreach (ProviderInfo info in providerInfos)
        {
            try
            {
                string url = "https://" + info.providerDomain + "/api/v3/oneprovider/data/" + id + "/content";

                StreamContent content = new StreamContent(stream);
                content.Headers.Add("Content-Type", "application/octet-stream");

                await OnedataPut(url, content);
                return;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        throw new Exception("Failed to put file");
    }

    public static async Task Move(List<ProviderInfo> providerInfos, string source, string dest, string spaceName)
    {
        foreach (ProviderInfo info in providerInfos)
        {
            try
            {
                string url = $"https://{info.providerDomain}/cdmi/{spaceName}/{dest}";

                string json = "{\"move\":\"" + spaceName + "/" + source + "\"}";
                StringContent content = new StringContent(json);
                content.Headers.Clear();
                content.Headers.Add("X-CDMI-Specification-Version", "1.1.1");
                content.Headers.Add("Content-type", "application/cdmi-object");

                await OnedataPut(url, content);
                return;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        throw new Exception("Failed to put file");
        /*
        def move(self, src_space_name: str, src_file_path: str, dst_space_name: str,
            dst_file_path: str) -> None:
        """Rename a file or directory."""
        # First create the target directory (this assumes that the src_file_path
        # already exists)
        headers = {
            "X-CDMI-Specification-Version": "1.1.1",
            "Content-type": "application/cdmi-object"
        }

        provider = self.get_provider_for_space(dst_space_name)
        url = f'https://{provider}/cdmi/{dst_space_name}/{dst_file_path}'

        data = {'move': f'{src_space_name}/{src_file_path}'}

        self.client.put(url, data=json.dumps(data), headers=headers)
        */

    }

    public static async Task<FileAttribute> GetFileAttribute(string fileId, string providerDomain)
    { 
        string url = "https://" 
                    + providerDomain
                    + "/api/v3/oneprovider/data/" 
                    + fileId;
        return await OnedataGet<FileAttribute>(url);
    }

    public static async Task<FileAttribute> GetFileAttribute(string fileId, List<ProviderInfo> providerInfos)
    {
        foreach (ProviderInfo info in providerInfos)
        {
            try
            {
                string url = "https://" 
                    + info.providerDomain
                    + "/api/v3/oneprovider/data/" 
                    + fileId;
                return await OnedataGet<FileAttribute>(url);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        throw new Exception("Failed to get FileInfo");
    }

    public static void Stop()
    {
        client.CancelPendingRequests();

        clientNoHeaders.CancelPendingRequests();
    }
}