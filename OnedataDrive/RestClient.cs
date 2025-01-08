using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Web;

namespace OnedataDrive
{
    static class RestClient
    {
        public static string PROVIDER_TOKEN = "";
        public static string ZONE_HOST = "";
        public static string ZONE_PROTOCOL = "";
        private static HttpClient client = new();
        private static HttpClient clientNoHeaders = new();
        private const string HTTP = "http://";
        private const string HTTPS = "https://";
        public static bool initialized { get; private set; } = false;

        public static void Init(Config config)
        {
            PROVIDER_TOKEN = config.provider_token;
            if (config.onezone.StartsWith(HTTPS))
            {
                ZONE_HOST = config.onezone.Substring(8);
                ZONE_PROTOCOL = HTTPS;
            }
            else if (config.onezone.StartsWith(HTTP))
            {
                ZONE_HOST = config.onezone.Substring(7);
                ZONE_PROTOCOL = HTTP;
            }
            else
            {
                ZONE_HOST = config.onezone;
                ZONE_PROTOCOL = HTTPS;
            }

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-auth-token", PROVIDER_TOKEN);

            clientNoHeaders.DefaultRequestHeaders.Clear();

            initialized = true;
        }

        public static void Stop()
        {
            client.CancelPendingRequests();

            clientNoHeaders.CancelPendingRequests();
            initialized = false;
        }

        private static async Task<T> OnedataGet<T>(string url)
        {
            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            T? data = JsonSerializer.Deserialize<T>(response.Content.ReadAsStream());
            response.Dispose();
            return data ??
                throw new JsonReturnedNullException("URL: " + url);
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
             throw new JsonReturnedNullException();
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

        /////////////////////////////////////////////////////////////////////////////


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

        public static async Task<TokenExamine> ExamineToken()
        {
            string url = ZONE_PROTOCOL
                + ZONE_HOST
                + "/api/v3/onezone/tokens/examine";

            string json = "{\"token\":\"" + PROVIDER_TOKEN + "\"}";
            StringContent content = new(json);
            content.Headers.Clear();
            content.Headers.Add("Content-Type", "application/json");

            var response = await clientNoHeaders.PostAsync(url, content);

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<TokenExamine>(response.Content.ReadAsStream()) ??
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
        }

        public static async Task Move(List<ProviderInfo> providerInfos, string source, string target)
        {
            foreach (ProviderInfo info in providerInfos)
            {
                try
                {
                    string escapedTarget = HttpEncodePath(target);
                    string url = $"https://{info.providerDomain}/cdmi/{escapedTarget}";

                    // backslash(\) and double quotes(") are special characters in JSON,
                    // so they must be escaped with \
                    string escapedSource = source.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    string json = "{\"move\":\"" + escapedSource + "\"}";
                    StringContent content = new StringContent(json);
                    content.Headers.Clear();
                    content.Headers.Add("X-CDMI-Specification-Version", "1.1.1");
                    content.Headers.Add("Content-type", "application/cdmi-object");

                    Debug.Print("URL: {0}", url);
                    Debug.Print("JSON: {0}", json);

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

        /// <summary>
        /// Takes correct URL and escapes special characters
        /// Special characters --> %,?,",#,[,],\
        /// This function is only correct for encoding path, which is passed to CDMI.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string HttpEncodePath(string path)
        {
            string encoded = path;
            // order is important (especialy % being first)
            encoded = encoded.Replace("%", "%25");
            encoded = encoded.Replace("?", "%3F");
            encoded = encoded.Replace("#", "%23");
            encoded = encoded.Replace("[", "%5B");
            encoded = encoded.Replace("]", "%5D");
            encoded = encoded.Replace("\\", "%5C");
            return encoded;
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
    }
}