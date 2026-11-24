using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.BioshockInfinite.Assets.Objects.Properties;

public class XWeakReference
{
    public FName Name1;
    public FName Name2;
    public FName Name3;
    public FName? Name4;
    public int? Extra;

    public XWeakReference() { }

    public XWeakReference(FAssetArchive Ar)
    {
        Name1 = Ar.ReadFName();
        Name2 = Ar.ReadFName();
        var moreData = Ar.Read<byte>();
        Name3 = Ar.ReadFName();
        if (moreData != 0)
        {
            Name4 = Ar.ReadFName();
            Extra = Ar.Read<int>();
        }
    }
}

[JsonConverter(typeof(XWeakReferencePropertyConverter))]
public class XWeakReferenceProperty : FPropertyTagType<XWeakReference>
{
    public XWeakReferenceProperty(FAssetArchive Ar, ReadType type)
    {
        Value = type switch
        {
            ReadType.ZERO => new XWeakReference(),
            _ => new XWeakReference(Ar)
        };
    }
}

public class XWeakReferencePropertyConverter : JsonConverter<XWeakReferenceProperty>
{
    public override void WriteJson(JsonWriter writer, XWeakReferenceProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override XWeakReferenceProperty ReadJson(JsonReader reader, Type objectType, XWeakReferenceProperty? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
