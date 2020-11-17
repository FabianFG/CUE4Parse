using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class StructProperty : FPropertyTagType<UScriptStruct>
    {
        public StructProperty(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type)
        {
            Value = new UScriptStruct(Ar, tagData?.StructType, type);
        }

        public override string ToString() => Value.ToString().SubstringBeforeLast(')') + ", StructProperty)";
    }
}