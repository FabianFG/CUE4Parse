using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class UTextureRenderTarget : UTexture
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Ver < EUnrealEngineObjectUE3Version.RENDERING_REFACTOR)
        {
            var SizeX = Ar.Read<int>();
            var SizeY = Ar.Read<int>();
            var format = Ar.Read<int>();
            Format = (EPixelFormat)format;
            var numMips = Ar.Read<int>();
        }
    }
}
