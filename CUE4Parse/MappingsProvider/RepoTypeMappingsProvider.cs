using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace CUE4Parse.MappingsProvider
{
    public sealed class RepoTypeMappingsProvider : JsonTypeMappingsProvider
    {
        public string Branch = "master";
        private string DownloadRepo => $"https://api.github.com/repos/FabianFG2/MappingTest/zipball/{Branch}";

        private HttpClient _client = new HttpClient { Timeout = TimeSpan.FromSeconds(2), DefaultRequestHeaders = { { "User-Agent", "CUE4Parse" } }};
        
        public RepoTypeMappingsProvider()
        {
            Reload();
        }
        
        private static JsonSerializerSettings _githubJsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy(false, false)
            }
        };

        public override bool Reload() => ReloadAsync().Result;

        public override async Task<bool> ReloadAsync()
        {

            // Fortnite
            const string game = "fortnitegame";
            var zip = await LoadEndpointBytes($"{DownloadRepo}?ref={Branch}");
            if (zip == null) return false;
            var zipAr = new ZipArchive(new MemoryStream(zip));
            foreach (var zipEntry in zipAr.Entries)
            {
                try
                {
                    if (zipEntry.Name.EndsWith("_ClassMappings.json"))
                    {
                        var sr = new StreamReader(zipEntry.Open());
                        if (sr.BaseStream.CanRead)
                            AddStructs(await sr.ReadToEndAsync(), game);
                    }
                    else if (zipEntry.Name.EndsWith("_StructMappings.json"))
                    {
                        var sr = new StreamReader(zipEntry.Open());
                        if (sr.BaseStream.CanRead)
                            AddStructs(await sr.ReadToEndAsync(), game);
                    }
                    else if (zipEntry.Name.EndsWith("_EnumMappings.json"))
                    {
                        var sr = new StreamReader(zipEntry.Open());
                        if (sr.BaseStream.CanRead)
                            AddEnums(await sr.ReadToEndAsync(), game);
                    }  
                }
                catch
                {
                    Log.Warning("Failed to load {0}", zipEntry.Name);
                }
            }
            
            zipAr.Dispose();
            return true;
        }

        private async Task<string?> LoadEndpoint(string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            try
            {
                var response = await _client.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }
        
        private async Task<byte[]?> LoadEndpointBytes(string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            try
            {
                var response = await _client.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch
            {
                return null;
            }
        }
    }
}