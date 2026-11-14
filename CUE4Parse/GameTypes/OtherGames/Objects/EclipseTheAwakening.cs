using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public class UXDataTable : UObject
{
    public Dictionary<int, FStructFallback>? Rows;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var ptr = GetOrDefault<FPackageIndex?>("RowStruct");
        if (ptr is null || !ptr.TryLoad<UStruct>(out var rowStruct))
        {
            Log.Warning("Can't find or load RowStruct type to serialize UXDataTable");
            return;
        }

        Rows = Ar.ReadMap(Ar.Read<int>, () => new FStructFallback(Ar, rowStruct));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Rows));
        serializer.Serialize(writer, Rows);
    }
}

[JsonConverter(typeof(FXDataTableRowValueConverter))]
public class FXDataTableRowValue : FStructFallback
{
    public FPropertyTagType?[][]? Value;

    public FXDataTableRowValue(FAssetArchive Ar) : base(Ar, "XDataTableRowValue")
    {
        Value = Ar.ReadArray(() => Ar.ReadArray(() => ReadXDataValue(Ar)));
    }

    private static FPropertyTagType? ReadXDataValue(FAssetArchive Ar)
    {
        var type = Ar.Read<EXDataTableRowValueType>();
        FPropertyTagType? tag = type switch
        {
            EXDataTableRowValueType.String => new StrProperty(Ar, ReadType.NORMAL),
            EXDataTableRowValueType.Integer => new Int64Property(Ar, ReadType.NORMAL),
            EXDataTableRowValueType.Boolean => new BoolProperty(Ar.ReadBoolean()),
            EXDataTableRowValueType.Name => new NameProperty(Ar, ReadType.NORMAL),
            EXDataTableRowValueType.Float => new FloatProperty(Ar, ReadType.NORMAL),
            EXDataTableRowValueType.Asset => new SoftObjectProperty(Ar, ReadType.NORMAL),
            EXDataTableRowValueType.Array => new ArrayProperty(new (Ar.ReadArray(() => ReadXDataValue(Ar)).ToList(), "", null)),
            _ => null
        };
        return tag;
    }

    private enum EXDataTableRowValueType: sbyte
    {
        None = -1,
        String = 0,
        Integer = 1,
        Boolean = 2,
        Name = 3,
        Float = 4,
        Asset = 5,
        Array = 6,
        Max = 7
    }
}

public class FXDataTableRowValueConverter : JsonConverter<FXDataTableRowValue>
{
    public override void WriteJson(JsonWriter writer, FXDataTableRowValue? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value?.Value);
    }

    public override FXDataTableRowValue ReadJson(JsonReader reader, Type objectType, FXDataTableRowValue? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
