using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.PCG;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.PCG;

public class UPCGMetadata : UObject
{
    public Dictionary<FName, FPCGMetadataAttributeBase> Attributes;
    public long[] ParentKeys;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Attributes = Ar.ReadMap(Ar.ReadFName, () => FPCGMetadataAttributeBase.ReadPCGMetadataAttribute(Ar));
        ParentKeys = Ar.ReadArray<long>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (Attributes?.Count > 0)
        {
            writer.WritePropertyName(nameof(Attributes));
            writer.WriteStartObject();
            foreach (var (key, value) in Attributes)
            {
                writer.WritePropertyName(key.Text);
                serializer.Serialize(writer, value);
            }
            writer.WriteEndObject();
        }
    }
}
