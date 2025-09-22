using System;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.Borderlands4.Assets.Objects.Properties;

public class FGbxDefPtrProperty : FProperty
{
    public FPackageIndex Struct;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        Struct = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Struct));
        serializer.Serialize(writer, Struct);
    }
}

public class FGbxDefPtr
{
    public FName Name;
    public FPackageIndex Struct;

    public FGbxDefPtr() { }

    public FGbxDefPtr(FAssetArchive Ar)
    {
        Name = Ar.ReadFName();
        Struct = new FPackageIndex(Ar);
    }

    public FGbxDefPtr(FName name, FPackageIndex structRef)
    {
        Name = name;
        Struct = structRef;
    }
}

[JsonConverter(typeof(GbxDefPtrPropertyConverter))]
public class GbxDefPtrProperty : FPropertyTagType<FGbxDefPtr>
{
    public GbxDefPtrProperty(FAssetArchive Ar, ReadType type)
    {
        Value = type switch
        {
            ReadType.ZERO => new FGbxDefPtr(),
            _ => new FGbxDefPtr(Ar)
        };
    }
}

public class GbxDefPtrPropertyConverter : JsonConverter<GbxDefPtrProperty>
{
    public override void WriteJson(JsonWriter writer, GbxDefPtrProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override GbxDefPtrProperty ReadJson(JsonReader reader, Type objectType, GbxDefPtrProperty? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FGameDataHandleProperty : FProperty
{
    public ulong Flags;

    public override void Deserialize(FAssetArchive Ar)
    {
        base.Deserialize(Ar);
        Flags = Ar.Read<ulong>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Flags));
        writer.WriteValue(Flags);
    }
}

public class FGameDataHandle
{
    public uint Flags;
    public FName Name;

    public FGameDataHandle() { }

    public FGameDataHandle(FAssetArchive Ar)
    {
        Name = Ar.ReadFName();
        Flags = Ar.Read<uint>();
    }

    public FGameDataHandle(uint flags, FName name)
    {
        Flags = flags;
        Name = name;
    }
}

[JsonConverter(typeof(GameDataHandlePropertyConverter))]
public class GameDataHandleProperty : FPropertyTagType<FGameDataHandle>
{
    public GameDataHandleProperty(FAssetArchive Ar, ReadType type)
    {
        Value = type switch
        {
            ReadType.ZERO => new FGameDataHandle(),
            _ => new FGameDataHandle(Ar)
        };
    }
}

public class GameDataHandlePropertyConverter : JsonConverter<GameDataHandleProperty>
{
    public override void WriteJson(JsonWriter writer, GameDataHandleProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override GameDataHandleProperty ReadJson(JsonReader reader, Type objectType, GameDataHandleProperty? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
