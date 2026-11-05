using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

[JsonConverter(typeof(StructPropertyConverter))]
public class StructProperty : FPropertyTagType<FScriptStruct>
{
    public StructProperty(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type)
    {
        var structType = tagData?.StructType;
        var pushedStructType = !string.IsNullOrEmpty(structType);
        if (pushedStructType) Ar.StructTypeStack.Push(structType!);
        try
        {
            Value = new FScriptStruct(Ar, tagData?.StructType, tagData?.Struct, type);
        }
        finally
        {
            if (pushedStructType) Ar.StructTypeStack.Pop();
        }
    }

    public StructProperty(FScriptStruct value) => Value = value;

    public override string ToString() => Value is null
        ? "(null struct)"
        : Value.ToString().SubstringBeforeLast(')') + ", StructProperty)";
}
