using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace CUE4Parse.UE4.Assets.Objects.Unversioned
{
    public abstract class JsonTypeMappingsProvider : AbstractTypeMappingsProvider
    {
        protected static JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(false, false)
            }
        };
        public override Dictionary<string, TypeMappings> MappingsByGame { get; protected set; } = new Dictionary<string, TypeMappings>();

        protected bool AddStructs(string structsJson, string game)
        {
            if (!MappingsByGame.TryGetValue(game, out TypeMappings mappingsForGame))
            {
                mappingsForGame = new TypeMappings();
                MappingsByGame[game] = mappingsForGame;
            }

            var token = JArray.Parse(structsJson);
            foreach (var structToken in token)
            {
                if (structToken == null) continue;
                var structEntry = new Struct(mappingsForGame, structToken);
                mappingsForGame.Types[structEntry.Name] = structEntry;
            }
            return true;
        }

        protected void AddEnums(string enumsJson, string game)
        {
            if (!MappingsByGame.TryGetValue(game, out TypeMappings mappingsForGame))
            {
                mappingsForGame = new TypeMappings();
                MappingsByGame[game] = mappingsForGame;
            }
            
            var token = JArray.Parse(enumsJson);
            foreach (var entry in token)
            {
                if (entry == null) continue;
                var values = entry["values"]!.ToObject<string[]>()!;
                var i = 0;
                mappingsForGame.Enums[entry["name"]!.ToObject<string>()!] = values.ToDictionary(it => i++);
            }
        }
    }
}