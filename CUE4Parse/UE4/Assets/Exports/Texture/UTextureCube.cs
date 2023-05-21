using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class UTextureCube : UTexture
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var stripFlags = new FStripDataFlags(Ar);
        var bCooked = Ar.ReadBoolean();

        if (bCooked)
        {
            DeserializeCookedPlatformData(Ar);
        }
    }
}
