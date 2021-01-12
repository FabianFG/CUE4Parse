using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CUE4Parse.MappingsProvider
{
    public class BenBotMappingsProvider : UsmapTypeMappingsProvider
    {

        public string? SpecificVersion = null;

        private readonly bool _isWindows64Bit;

        public BenBotMappingsProvider(string? specificVersion = null)
        {
            SpecificVersion = specificVersion;
            _isWindows64Bit = Environment.Is64BitOperatingSystem && RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Reload();
        }
        
        public const string BenMappingsEndpoint = "https://benbotfn.tk/api/v1/mappings";
        
        private HttpClient _client = new HttpClient { Timeout = TimeSpan.FromSeconds(2), DefaultRequestHeaders = { { "User-Agent", "CUE4Parse" } }};
        
        public sealed override bool Reload()
        {
            return ReloadAsync().GetAwaiter().GetResult();
        }

        public sealed override async Task<bool> ReloadAsync()
        {
            try
            {
                var jsonText = SpecificVersion != null
                    ? await LoadEndpoint(BenMappingsEndpoint + $"?version={SpecificVersion}")
                    : await LoadEndpoint(BenMappingsEndpoint);
                if (jsonText == null)
                {
                    Log.Warning("Failed to get BenBot Mappings Endpoint");
                    return false;
                }
                var json =  JArray.Parse(jsonText);
                var preferredCompression = _isWindows64Bit ? "Oodle" : "Brotli";

                if (!json.HasValues)
                {
                    Log.Warning("Couldn't reload mappings, json array was empty");
                    return false;
                }

                string? usmapUrl = null;
                string? usmapName = null;
                foreach (var arrayEntry in json)
                {
                    var method = arrayEntry["meta"]?["compressionMethod"]?.ToString();
                    if (method != null && method == preferredCompression)
                    {
                        usmapUrl = arrayEntry["url"]?.ToString();
                        usmapName = arrayEntry["fileName"]?.ToString();
                        break;
                    }
                }

                if (usmapUrl == null)
                {
                    usmapUrl = json[0]["url"]?.ToString()!;
                    usmapName = json[0]["fileName"]?.ToString()!;
                }

                var usmapBytes = await LoadEndpointBytes(usmapUrl);
                if (usmapBytes == null)
                {
                    Log.Warning("Failed to download usmap");
                    return false;
                }

                AddUsmap(usmapBytes, "fortnite", usmapName!);
                return true;
            }
            catch (Exception e)
            {
                Log.Warning(e, "Uncaught exception while reloading mappings from BenBot");
                return false;
            }
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