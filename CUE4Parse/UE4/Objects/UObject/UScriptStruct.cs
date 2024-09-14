using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.UObject;

public class UScriptStruct : UStruct
{
    public EStructFlags StructFlags;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        StructFlags = Ar.Read<EStructFlags>();
    }
}
