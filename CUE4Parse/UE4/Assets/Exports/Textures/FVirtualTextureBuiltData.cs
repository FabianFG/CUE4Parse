using CUE4Parse.UE4.Assets.Readers;
using System;

namespace CUE4Parse.UE4.Assets.Exports.Textures
{
    public class FVirtualTextureBuiltData
    {
        public readonly uint NumLayers;
        public readonly uint? NumMips;
        public readonly uint? Width;
        public readonly uint? Height;
        public readonly uint WidthInBlocks;
        public readonly uint HeightInBlocks;
        public readonly uint TileSize;
        public readonly uint TileBorderSize;
        public readonly EPixelFormat[] LayerTypes;
        public readonly FVirtualTextureDataChunk[] Chunks;
        public readonly uint[]? TileIndexPerChunk;
        public readonly uint[]? TileIndexPerMip;
        public readonly uint[]? TileOffsetInChunk;

        public FVirtualTextureBuiltData(FAssetArchive Ar, int firstMip)
        {
            bool bStripMips = firstMip > 0;
            var bCooked = Ar.ReadBoolean();

            NumLayers = Ar.Read<uint>();
            WidthInBlocks = Ar.Read<uint>();
            HeightInBlocks = Ar.Read<uint>();
            TileSize = Ar.Read<uint>();
            TileBorderSize = Ar.Read<uint>();
            if (!bStripMips)
            {
                NumMips = Ar.Read<uint>();
                Width = Ar.Read<uint>();
                Height = Ar.Read<uint>();
                TileIndexPerChunk = Ar.ReadArray<uint>();
                TileIndexPerMip = Ar.ReadArray<uint>();
                TileOffsetInChunk = Ar.ReadArray<uint>();
            }

            LayerTypes = Ar.ReadArray((int)NumLayers, () => (EPixelFormat)Enum.Parse(typeof(EPixelFormat), Ar.ReadFString()));
            Chunks = Ar.ReadArray(Ar.Read<int>(), () => new FVirtualTextureDataChunk(Ar, NumLayers));
        }
    }
}
