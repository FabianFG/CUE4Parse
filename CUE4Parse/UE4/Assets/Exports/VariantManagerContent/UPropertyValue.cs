using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.VariantManagerContent;

public class UPropertyValue : UObject
{
    public FName? LeafPropertyClass;
    public FSoftObjectPath TempObjPtr;
    public FName? TempName;
    public string? TempStr;
    public FText? TempText;

    override public void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (FCoreObjectVersion.Get(Ar) >= FCoreObjectVersion.Type.FProperties)
        {
            LeafPropertyClass = Ar.ReadFName();
        }

        TempObjPtr = new FSoftObjectPath(Ar);

        if (FVariantManagerObjectVersion.Get(Ar) >= FVariantManagerObjectVersion.Type.CorrectSerializationOfFStringBytes)
        {
            TempName = Ar.ReadFName();
            TempStr = Ar.ReadFString();
            TempText = new FText(Ar);
        }
        else if (FVariantManagerObjectVersion.Get(Ar) >= FVariantManagerObjectVersion.Type.CorrectSerializationOfFNameBytes)
        {
            TempName = Ar.ReadFName();
        }
    }

    protected internal override void WriteJson(Newtonsoft.Json.JsonWriter writer, Newtonsoft.Json.JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (LeafPropertyClass is not null)
        {
            writer.WritePropertyName("LeafPropertyClass");
            serializer.Serialize(writer, LeafPropertyClass);
        }

        writer.WritePropertyName("TempObjPtr");
        serializer.Serialize(writer, TempObjPtr);

        if (TempName is not null)
        {
            writer.WritePropertyName("TempName");
            serializer.Serialize(writer, TempName);
        }
        if (TempStr is not null)
        {
            writer.WritePropertyName("TempStr");
            serializer.Serialize(writer, TempStr);
        }
        if (TempText is not null)
        {
            writer.WritePropertyName("TempText");
            serializer.Serialize(writer, TempText);
        }
    }
}
