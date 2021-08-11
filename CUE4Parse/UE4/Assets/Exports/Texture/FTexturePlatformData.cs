using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Texture
{
    public class FTexturePlatformData
    {
        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int NumSlices; // 1 for simple texture, 6 for cubemap - 6 textures are joined into one
        public readonly string PixelFormat;
        public readonly int FirstMip;
        public readonly FTexture2DMipMap[] Mips;
        public readonly bool bIsVirtual;
        public readonly FVirtualTextureBuiltData? VTData;

        public FTexturePlatformData(FAssetArchive Ar)
        {
            if (Ar.Game == EGame.GAME_PlayerUnknownsBattlegrounds)
            {
                SizeX = Ar.Read<short>();
                SizeY = Ar.Read<short>();
                var data = Ar.ReadBytes(3); // int24
                NumSlices = data[0] + (data[1] << 8) + (data[2] << 16);
            }
            else
            {
                SizeX = Ar.Read<int>();
                SizeY = Ar.Read<int>();
                NumSlices = Ar.Read<int>();
            }

            PixelFormat = Ar.ReadFString();

            FirstMip = Ar.Read<int>(); // only for cooked, but we don't read FTexturePlatformData for non-cooked textures
            Mips = Ar.ReadArray(() => new FTexture2DMipMap(Ar));

            if (Ar.Versions["VirtualTextures"])
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