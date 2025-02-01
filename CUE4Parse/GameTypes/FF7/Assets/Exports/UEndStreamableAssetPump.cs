using System;
using System.Collections.Generic;
using CUE4Parse.GameTypes.FF7.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.FF7.Assets.Exports;

public struct FF7AssetPumpStruct1
{
    public ushort Index;
    public ushort Type;
}

public struct FF7AssetPumpStruct2(FMemoryMappedImageArchive Ar)
{
    public byte[][] idk = Ar.ReadArray(Ar.ReadArray<byte>, 1);
    public int Type = Ar.Read<int>();
}

[JsonConverter(typeof(FF7StreamableAssetPumpKeyConverter))]
public struct FF7StreamableAssetPumpKey(FMemoryMappedImageArchive Ar)
{
    public FF7AssetPumpStruct1[] idk1 = Ar.ReadArray<FF7AssetPumpStruct1>();
    public FF7AssetPumpStruct2[] idk2 = Ar.ReadArray(() => new FF7AssetPumpStruct2(Ar));
    public int Index = Ar.Read<int>();
}

public class FF7StreamableAssetPumpKeyConverter : JsonConverter<FF7StreamableAssetPumpKey>
{
    public override void WriteJson(JsonWriter writer, FF7StreamableAssetPumpKey value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Index);
    }

    public override FF7StreamableAssetPumpKey ReadJson(JsonReader reader, Type objectType, FF7StreamableAssetPumpKey existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class UEndStreamableAssetPump : UMemoryMappedAsset
{
    public FF7StreamableAssetPumpKey[] Sections = [];
    public Dictionary<FF7StreamableAssetPumpKey, Dictionary<string, ushort>> AssetMap = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Sections = InnerArchive.ReadArray(() => new FF7StreamableAssetPumpKey(InnerArchive));
        var Assets = InnerArchive.ReadArray(InnerArchive.ReadFString);

        foreach (var section in Sections)
        {
            AssetMap[section] = new Dictionary<string, ushort>();
            for (var i = 0; i < section.idk1.Length; i++)
            {
                AssetMap[section][Assets[section.idk1[i].Index]] = section.idk1[i].Type;
            }
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        foreach (var section in Sections)
        {
            writer.WritePropertyName(section.Index.ToString());
            serializer.Serialize(writer, AssetMap[section]);
        }
    }
}
