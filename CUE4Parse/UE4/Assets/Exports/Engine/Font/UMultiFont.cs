using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Engine.Font;

public class UMultiFont : UFont
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Ver < EUnrealEngineObjectUE3Version.FIXED_FONTS_SERIALIZATION)
        {
            Ar.SkipFixedArray(sizeof(int)); // ResolutionTestTable
        }
    }
}
