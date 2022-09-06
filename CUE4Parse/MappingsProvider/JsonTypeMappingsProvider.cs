using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace CUE4Parse.MappingsProvider
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

        public override TypeMappings? MappingsForGame { get; protected set; }

        protected bool AddStructs(string structsJson)
        {
            MappingsForGame ??= new TypeMappings();

            var token = JArray.Parse(structsJson);
            foreach (var structToken in token)
            {
                if (structToken == null) continue;
                var structEntry = ParseStruct(MappingsForGame, structToken);
                MappingsForGame.Types[structEntry.Name] = structEntry;
            }
            return true;
        }

        private Struct ParseStruct(TypeMappings context, JToken structToken)
        {
            var name = structToken["name"]!.ToObject<string>()!;
            var superType = structToken["superType"]?.ToObject<string>();

            var propertiesToken = (JArray) structToken["properties"]!;
            var properties = new Dictionary<int, PropertyInfo>();
            foreach (var propToken in propertiesToken)
            {
                if (propToken == null) continue;
                var prop = ParsePropertyInfo(propToken);
                for (int i = 0; i < prop.ArraySize; i++)
                {
                    properties[prop.Index + i] = prop;
                }
            }
            var propertyCount = structToken["propertyCount"]!.ToObject<int>()!;

            return new Struct(context, name, superType, properties, propertyCount);
        }

        private PropertyInfo ParsePropertyInfo(JToken propToken)
        {
            var index = propToken["index"]!.ToObject<int>()!;
            var name = propToken["name"]!.ToObject<string>()!;
            var arraySize = propToken["arraySize"]?.ToObject<int>();
            var mappingType = ParsePropertyType(propToken["mappingType"]!);
            return new PropertyInfo(index, name, mappingType, arraySize);
        }

        private PropertyType ParsePropertyType(JToken typeToken)
        {
            var Type = typeToken["type"]!.ToObject<string>()!;
            var StructType = typeToken["structType"]?.ToObject<string>();
            var innerTypeToken = typeToken["innerType"];
            var InnerType = innerTypeToken != null ? ParsePropertyType(innerTypeToken) : null;
            var valueTypeToken = typeToken["valueType"];
            var ValueType = valueTypeToken != null ? ParsePropertyType(valueTypeToken) : null;
            var EnumName = typeToken["enumName"]?.ToObject<string>();
            var IsEnumAsByte = typeToken["isEnumAsByte"]?.ToObject<bool>();
            return new PropertyType(Type, StructType, InnerType, ValueType, EnumName, IsEnumAsByte);
        }

        protected void AddEnums(string enumsJson)
        {
            MappingsForGame ??= new TypeMappings();

            var token = JArray.Parse(enumsJson);
            foreach (var entry in token)
            {
                if (entry == null) continue;
                var values = entry["values"]!.ToObject<string[]>()!;
                var i = 0;
                MappingsForGame.Enums[entry["name"]!.ToObject<string>()!] = values.ToDictionary(it => i++);
            }
        }
    }
}
