using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CUE4Parse.UE4.Assets.Objects.Unversioned
{
    public abstract class JsonTypeMappingsContainer : AbstractTypeMappingsContainer
    {
        private static JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(false, false)
            }
        };
        public override IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<int, PropertyInfo>>> TypesByGame { get; protected set; }
        public override IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>>> EnumsByGame { get; protected set; }

        protected bool Reload(IReadOnlyDictionary<string, string> typesJson, IReadOnlyDictionary<string, string> enumsJson)
        {
            var failed = false;
            
            var typesByGame = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<int, PropertyInfo>>>();
            foreach (var pair in typesJson)
            {
                var types = JsonConvert.DeserializeObject<IReadOnlyDictionary<string, IReadOnlyDictionary<int, PropertyInfo>>>(
                    pair.Value, _serializerSettings);

                if (types == null)
                {
                    failed = true;  
                    typesByGame[pair.Key] = new Dictionary<string, IReadOnlyDictionary<int, PropertyInfo>>();
                }
                else
                    typesByGame[pair.Key] = types;
            }
            
            var enumsByGame = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>>>();
            foreach (var pair in enumsJson)
            {
                var enums = JsonConvert.DeserializeObject<IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>>>(
                    pair.Value, _serializerSettings);

                if (enums == null)
                {
                    failed = true;
                    enumsByGame[pair.Key] = new Dictionary<string, IReadOnlyDictionary<int, string>>();
                }
                else
                    enumsByGame[pair.Key] = enums;
            }

            TypesByGame = typesByGame;
            EnumsByGame = enumsByGame;
            return !failed;
        }
    }
}