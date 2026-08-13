using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.RED.Assets.Exports;

public class UREDBinaryObject : UObject
{
    public FByteBulkData DataBE;
    public FByteBulkData DataLE;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
#if DEBUG
        Log.Debug("{0}", GetType().Name);
#endif
        DataBE = new FByteBulkData(Ar);
        DataLE = new FByteBulkData(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(DataBE));
        serializer.Serialize(writer, DataBE);

        writer.WritePropertyName(nameof(DataLE));
        serializer.Serialize(writer, DataLE);
    }
}

public class UREDLipSyncData : UREDBinaryObject;
public class UREDLibraryTextData : UREDBinaryObject;

public class UREDLocalizeTextData : UREDBinaryObject
{
    public Dictionary<string, string> LocalizedText;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        using var stream = new MemoryStream(DataLE.Data ?? []);
        using var reader = new StreamReader(stream);
        LocalizedText = [];
        while (reader.ReadLine() is { } key && reader.ReadLine() is { } value)
        {
            LocalizedText[key] = value;
        };
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(LocalizedText));
        serializer.Serialize(writer, LocalizedText);
    }
}
