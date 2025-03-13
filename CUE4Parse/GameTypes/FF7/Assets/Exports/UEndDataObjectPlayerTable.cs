using System.Collections.Generic;
using System.Linq;
using CUE4Parse.GameTypes.FF7.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.FF7.Assets.Exports;

public struct FKey(FMemoryImageArchive Ar)
{
    public FName Name = Ar.ReadFName();
    public int Index = Ar.Read<int>();
    public int NextIndex = Ar.Read<int>();
    public int Priority = Ar.Read<int>();
}

public struct FF7Property(FMemoryImageArchive Ar)
{
    public FName Name = Ar.ReadFName();
    public FF7propertyType UnderlyingType = (FF7propertyType)Ar.Read<int>();
}

public class UEndDataObjectBase : UMemoryMappedAsset
{
    public Dictionary<FKey, FF7StructProperty> DataTable = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var keys = InnerArchive.ReadTSparseArray(() => new FKey(InnerArchive), 20).ToArray();

        var Indexes = InnerArchive.ReadArray<int>();
        var structDefinition = InnerArchive.ReadArray(() => new FF7Property(InnerArchive));
        var values = InnerArchive.ReadArray(() => InnerArchive.DeserializeProperties(structDefinition));
        for (var i = 0; i < keys.Length; i++)
        {
            DataTable[keys[i]] = values[i];
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("DataTable");
        writer.WriteStartObject();
        foreach (var (key, value) in DataTable)
        {
            writer.WritePropertyName(key.Name.ToString());
            serializer.Serialize(writer, value);
        }
        writer.WriteEndObject();
    }
}
