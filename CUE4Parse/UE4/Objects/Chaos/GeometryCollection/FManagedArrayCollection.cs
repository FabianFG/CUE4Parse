using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

[JsonConverter(typeof(FManagedArrayCollectionConverter))]
public class FManagedArrayCollection
{
    public int Version;
    public Dictionary<FName, FGroupInfo> GroupInfo;
    public Dictionary<FKeyType, FValueType> Map;

    public FManagedArrayCollection(FChaosArchive Ar)
    {
        Version = Ar.Read<int>();
        GroupInfo = Ar.ReadMap(Ar.ReadFName, () => new FGroupInfo(Ar));
        Map = Ar.ReadMap(() => new FKeyType(Ar), () => new FValueType(Ar));
    }

    public T[]? GetAttributeValue<T>(string attribute, string group) => GetAttributeValue<T>(attribute, new FName(group));
    public T[]? GetAttributeValue<T>(string attribute, FName group) => GetAttributeValue<T>(new FKeyType(attribute, group));
    public T[]? GetAttributeValue<T>(FKeyType key)
    {
        return Map.TryGetValue(key, out var value) ? value.ManagedArray.Data as T[] : null;
    }
}

public class FManagedArrayCollectionConverter : JsonConverter<FManagedArrayCollection>
{
    public override void WriteJson(JsonWriter writer, FManagedArrayCollection? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName(nameof(FManagedArrayCollection.Version));
        writer.WriteValue(value.Version);

        writer.WritePropertyName(nameof(FManagedArrayCollection.GroupInfo));
        serializer.Serialize(writer, value.GroupInfo);

        writer.WritePropertyName(nameof(FManagedArrayCollection.Map));
        writer.WriteStartObject();
        foreach (var kvp in value.Map)
        {
            writer.WritePropertyName(kvp.Key.ToString());
            serializer.Serialize(writer, kvp.Value.ArrayType);
        }
        writer.WriteEndObject();

        writer.WriteEndObject();
    }
    public override FManagedArrayCollection? ReadJson(JsonReader reader, Type objectType, FManagedArrayCollection? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
