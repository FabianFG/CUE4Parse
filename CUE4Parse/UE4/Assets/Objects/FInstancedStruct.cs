using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(FInstancedStructConverter))]
public class FInstancedStruct : IUStruct
{
    public readonly FStructFallback NonConstStruct;

    public FInstancedStruct(FAssetArchive Ar)
    {
        var version = Ar.Read<byte>();

        var struc = new FPackageIndex(Ar);
        var serialSize = Ar.Read<int>();

        if (struc.IsNull && serialSize > 0)
        {
            Ar.Position += serialSize;
        }
        else
        {
            NonConstStruct = new FStructFallback(Ar, struc.Name);
        }
    }
}

public class FInstancedStructConverter : JsonConverter<FInstancedStruct>
{
    public override void WriteJson(JsonWriter writer, FInstancedStruct value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.NonConstStruct);
    }

    public override FInstancedStruct ReadJson(JsonReader reader, Type objectType, FInstancedStruct existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}