using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Texture
{
    public struct FOptTexturePlatformData
    {
        public uint ExtData;
        public uint NumMipsInTail;
    }

    public class FTexturePlatformData
    {
        private const uint BitMask_CubeMap    = 1u << 31;
        private const uint BitMask_HasOptData = 1u << 30;
        private const uint BitMask_NumSlices  = BitMask_HasOptData - 1u;

        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int PackedData; // NumSlices: 1 for simple texture, 6 for cubemap - 6 textures are joined into one
        public readonly string PixelFormat;
        public readonly FOptTexturePlatformData OptData;
        public readonly int FirstMipToSerialize;
        public readonly FTexture2DMipMap[] Mips;
        public readonly FVirtualTextureBuiltData? VTData;

        public FTexturePlatformData(FAssetArchive Ar)
        {
            if (Ar is { Game: >= EGame.GAME_UE5_0, IsFilterEditorOnly: true })
            {
                const long PlaceholderDerivedDataSize = 16;
                Ar.Position += PlaceholderDerivedDataSize;
            }

            if (Ar.Game == EGame.GAME_PlayerUnknownsBattlegrounds)
            {
                SizeX = Ar.Read<short>();
                SizeY = Ar.Read<short>();
                var data = Ar.ReadBytes(3); // int24
                PackedData = data[0] + (data[1] << 8) + (data[2] << 16);
            }
            else
            {
                SizeX = Ar.Read<int>();
                SizeY = Ar.Read<int>();
                PackedData = Ar.Read<int>();
            }

            PixelFormat = Ar.Game == EGame.GAME_GearsOfWar4 ? Ar.ReadFName().Text : Ar.ReadFString();

            if (Ar.Game == EGame.GAME_FinalFantasy7Remake && (PackedData & 0xffff) == 16384)
            {
                var unk0 = Ar.Read<int>();
                var unk1 = Ar.Read<int>();
                var mapNum = Ar.Read<int>();
            }

            if ((PackedData & BitMask_HasOptData) == BitMask_HasOptData)
            {
                OptData = Ar.Read<FOptTexturePlatformData>();
            }

            FirstMipToSerialize = Ar.Read<int>(); // only for cooked, but we don't read FTexturePlatformData for non-cooked textures

            var mipCount = Ar.Read<int>();
            if (Ar.Platform == ETexturePlatform.Playstation && mipCount != 1) mipCount /= 3; // TODO: Some mips are corrupted, so this doesn't work 100% of the time.

            if (Ar.Game == EGame.GAME_FinalFantasy7Remake)
            {
                var firstMip = new FTexture2DMipMap(Ar);
                var val = Ar.Read<int>();
                if (val != PackedData)
                {
                    // oh no
                }

                Ar.Position += 4;
            }

            Mips = new FTexture2DMipMap[mipCount];
            for (var i = 0; i < Mips.Length; i++)
            {
                Mips[i] = new FTexture2DMipMap(Ar);
            }

            if (Ar.Versions["VirtualTextures"])
            {
                var bIsVirtual = Ar.ReadBoolean();
                if (bIsVirtual)
                {
                    VTData = new FVirtualTextureBuiltData(Ar, FirstMipToSerialize);
                }
            }
        }
    }
}