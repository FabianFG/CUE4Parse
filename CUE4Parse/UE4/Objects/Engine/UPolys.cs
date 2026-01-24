using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine;

public class UPolys : Assets.Exports.UObject
{
    public FPoly[] Element;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Ver < EUnrealEngineObjectUE4Version.BSP_UNDO_FIX)
        {
            var dbNum = Ar.Read<int>();
            var dbMax = Ar.Read<int>();

            _ = new FPackageIndex(Ar);
            Element = Ar.ReadArray(dbNum, () => new FPoly(Ar));
        }
        else
        {
            Element = Ar.ReadArray(() => new FPoly(Ar));
        }
    }
}