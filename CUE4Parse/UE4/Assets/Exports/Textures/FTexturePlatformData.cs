using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Textures
{
    public class FTexturePlatformData
    {
        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int NumSlices;        // 1 for simple texture, 6 for cubemap - 6 textures are joined into one
        public readonly string PixelFormat;
        public readonly int FirstMip;
        public readonly FTexture2DMipMap[] Mips;
        public readonly bool bIsVirtual;
        public readonly FVirtualTextureBuiltData? VTData;

        public FTexturePlatformData(FAssetArchive Ar)
        {
            SizeX = Ar.Read<int>();
            SizeY = Ar.Read<int>();
            NumSlices = Ar.Read<int>();

            PixelFormat = Ar.ReadFString();

            FirstMip = Ar.Read<int>();       // only for cooked, but we don't read FTexturePlatformData for non-cooked textures
            Mips = Ar.ReadArray(() => new FTexture2DMipMap(Ar));

            if (Ar.Game >= EGame.GAME_UE4_23) // bIsVirtual
            {
                bIsVirtual = Ar.ReadBoolean();
                if (bIsVirtual)
                {
                    VTData = new FVirtualTextureBuiltData(Ar, FirstMip);
                }
            }
        }
    }
}