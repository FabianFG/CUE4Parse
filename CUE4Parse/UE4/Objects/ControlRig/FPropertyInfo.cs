using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.ControlRig;

[JsonConverter(typeof(FPropertyInfoJsonConverter))]
public class FPropertyInfo
{
    public FPackageIndex Owner;
    public FFieldPath Property;
    public int ArrayIndex;
    
    public int Size;
    public uint Hash;
    public FPropertyTag? Value;

    public FPropertyInfo(FAssetArchive Ar)
    {
        Owner = new FPackageIndex(Ar);
        Property = new FFieldPath(Ar);
        ArrayIndex = Ar.Read<int>();
    }
}

public class FPropertyInfoJsonConverter : JsonConverter<FPropertyInfo>
{
    public override FPropertyInfo? ReadJson(JsonReader reader, Type objectType, FPropertyInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, FPropertyInfo? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.Owner));
        serializer.Serialize(writer, value.Owner);

        writer.WritePropertyName(nameof(value.Property));
        serializer.Serialize(writer, value.Property);

        writer.WritePropertyName(nameof(value.Size));
        writer.WriteValue(value.Size);

        writer.WritePropertyName(nameof(value.Hash));
        writer.WriteValue(value.Hash);

        if (value.Value is not null)
        {
            writer.WritePropertyName(nameof(value.Value));
            writer.WriteStartObject();
            writer.WritePropertyName(value.ArrayIndex > 0 ? $"{value.Value.Name.Text}[{value.ArrayIndex}]" : value.Value.Name.Text);
            serializer.Serialize(writer, value.Value.Tag);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
