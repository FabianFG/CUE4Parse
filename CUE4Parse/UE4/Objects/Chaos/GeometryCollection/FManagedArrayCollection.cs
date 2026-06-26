using System.Diagnostics;
using CUE4Parse.UE4.Assets.Exports.Chaos;
using CUE4Parse.UE4.Assets.Exports.GeometryCollection;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;


[DebuggerDisplay("Size: {Size}")]
public struct FGroupInfo
{
    public int Size;

    public FGroupInfo(FArchive Ar)
    {
        // var int version 4
        var version = Ar.Read<int>();
        Size = Ar.Read<int>();
    }
}

[JsonConverter(typeof(FManagedArrayCollectionConverter))]
public class FManagedArrayCollection
{
    public readonly int Version;
    public readonly Dictionary<FName, FGroupInfo> GroupInfo;       // FGroupInfo
    public readonly Dictionary<FKeyType, FValueType> Map;

    public FManagedArrayCollection(FChaosArchive Ar)
    {
        Version = Ar.Read<int>();

        var mapLength = Ar.Read<int>();
        GroupInfo = new Dictionary<FName, FGroupInfo>(mapLength);
        for (int i = 0; i < mapLength; i++)
        {
            GroupInfo[Ar.ReadFName()] = new FGroupInfo(Ar);
        }

        mapLength = Ar.Read<int>();
        Map = new Dictionary<FKeyType, FValueType>(mapLength);
        for (int i = 0; i < mapLength; i++)
        {
            var key = new FKeyType(Ar);
            Map[key] = new FValueType(Ar, Version);
        }
    }
}

public class FManagedArrayCollectionConverter : JsonConverter<FManagedArrayCollection>
{

    public override void WriteJson(JsonWriter writer, FManagedArrayCollection? value, JsonSerializer serializer)
    {        if (value == null)
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
        writer.WriteStartArray();
        // Key: {}, Value: {}
        foreach (var kvp in value.Map)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Key");
            serializer.Serialize(writer, kvp.Key);
            writer.WritePropertyName("Value");
            serializer.Serialize(writer, kvp.Value);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
    public override FManagedArrayCollection? ReadJson(JsonReader reader, Type objectType, FManagedArrayCollection? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
