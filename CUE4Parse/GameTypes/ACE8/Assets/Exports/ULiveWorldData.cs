using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.ACE8.Assets.Exports;

public class ULiveWorldData : UDataAsset
{
    public int[] Index;
    public FACE8Description[] Description;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Index = Ar.ReadArray<int>();
        Description = FACE8Description.ReadDescriptionArray(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(Index));
        serializer.Serialize(writer, Index);
        writer.WritePropertyName(nameof(Description));
        serializer.Serialize(writer, Description);
    }
}

public struct FACE8Description
{
    public FName Name;
    public byte Type;
    public int UnitCount;
    public FACE8Description[] Inner;

    public FACE8Description(FAssetArchive Ar)
    {
        Name = Ar.ReadFName();
        Type = Ar.Read<byte>();
        UnitCount = Ar.Read<int>();
        Inner = ReadDescriptionArray(Ar);
    }

    public static FACE8Description[] ReadDescriptionArray(FAssetArchive Ar)
    {
        var arrayNum =  Ar.Read<int>();
        if (arrayNum == 0)
        {
            return [];
        }
        var arrayMax =  Ar.Read<int>();
        if (arrayNum != arrayMax)
        {
            throw new ParserException(Ar, $"FACE8Description Num ({arrayNum}) != Max ({arrayMax})");
        }

        return Ar.ReadArray(arrayNum, () => new FACE8Description(Ar));
    }
}

public class ULiveEventPackage : UDataAsset
{
    public FStructFallback[] CustomEventTriggerTable;
    public FStructFallback[] CustomEventExecutionTable;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        CustomEventTriggerTable = Ar.ReadArray(() => new FStructFallback(Ar, "LiveCustomEventTriggerTable"));
        CustomEventExecutionTable = Ar.ReadArray(() => new FStructFallback(Ar, "LiveCustomEventExecutionTable"));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(CustomEventTriggerTable));
        serializer.Serialize(writer, CustomEventTriggerTable);
        writer.WritePropertyName(nameof(CustomEventExecutionTable));
        serializer.Serialize(writer, CustomEventExecutionTable);
    }
}
