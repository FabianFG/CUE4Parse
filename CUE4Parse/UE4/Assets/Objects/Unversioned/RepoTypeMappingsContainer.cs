using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CUE4Parse.UE4.Assets.Objects.Unversioned
{
    public sealed class RepoTypeMappingsContainer : JsonTypeMappingsContainer
    {
        public const string FortniteTypeMappings = "https://raw.githubusercontent.com/FabianFG/FortniteTypeMappings/master/TypeMappings.json";
        public const string FortniteEnumMappings = "https://raw.githubusercontent.com/FabianFG/FortniteTypeMappings/master/EnumMappings.json";
        
        private HttpClient _client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        
        public RepoTypeMappingsContainer()
        {
            Reload();
        }

        public override bool Reload() => ReloadAsync().Result;

        public override async Task<bool> ReloadAsync()
        {
            var typeJsons = new Dictionary<string, string>();
            var enumJsons = new Dictionary<string, string>();
            
            // Fortnite
            var fortniteTypes = await LoadEndpoint(FortniteTypeMappings);
            var fortniteEnums = await LoadEndpoint(FortniteEnumMappings);
            if (fortniteTypes != null)
                typeJsons["fortnite"] = fortniteTypes;
            if (fortniteEnums != null)
                enumJsons["fortnite"] = fortniteEnums;
            
            
            return Reload(typeJsons, enumJsons);
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
    }
}