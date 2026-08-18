using System.Diagnostics.CodeAnalysis;
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

    public bool TryGetAttributeValue<T>(FName attribute, FName group, [NotNullWhen(true)] out T[]? value) => TryGetAttributeValue(new FKeyType(attribute, group), out value);
    public bool TryGetAttributeValue<T>(FKeyType key, [NotNullWhen(true)] out T[]? value)
    {
        value = FindAttributeValue<T>(key, out _);
        return value != null;
    }

    public T[] GetAttributeValue<T>(FName attribute, FName group) => GetAttributeValue<T>(new FKeyType(attribute, group));
    public T[] GetAttributeValue<T>(FKeyType key)
    {
        if (FindAttributeValue<T>(key, out var exists) is { } value) return value;

        throw exists
            ? new InvalidCastException($"Attribute {key.Name} in group {key.Group} is not of type {typeof(T).Name}[]")
            : new KeyNotFoundException($"Attribute {key.Name} in group {key.Group} not found");
    }

    private T[]? FindAttributeValue<T>(FKeyType key, out bool exists)
    {
        exists = Map.TryGetValue(key, out var entry);
        return exists ? entry.ManagedArray.Data as T[] : null;
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
            writer.WriteValue($"{kvp.Value.ArrayType}, Length: {kvp.Value.ManagedArray?.Data?.Length ?? 0}");
        }
        writer.WriteEndObject();

        writer.WriteEndObject();
    }
    public override FManagedArrayCollection? ReadJson(JsonReader reader, Type objectType, FManagedArrayCollection? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
