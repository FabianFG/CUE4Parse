using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public class FTextureLookup
{
    public int TexCoordIndex;
    public int TextureIndex; // Index into Uniform2DTextureExpressions
    public float UScale;
    public float VScale;

    public FTextureLookup(FAssetArchive Ar)
    {
        TexCoordIndex = Ar.Read<int>();
        TextureIndex = Ar.Read<int>();

        if (Ar.Ver < EUnrealEngineObjectUE3Version.FONT_FORMAT_AND_UV_TILING_CHANGES)
        {
            var uAndVScale = Ar.Read<float>();
            UScale = uAndVScale;
            VScale = uAndVScale;
        }
        else
        {
            UScale = Ar.Read<float>();
            VScale = Ar.Read<float>();
        }
    }
}
