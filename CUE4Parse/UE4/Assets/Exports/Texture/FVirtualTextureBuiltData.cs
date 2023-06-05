using System;
using System.Diagnostics;
using System.Linq;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Texture
{
    public struct FVirtualTextureTileOffsetData : IUStruct
    {
        public uint Width;
        public uint Height;
        public uint MaxAddress;
        public uint[] Addresses;
        public uint[] Offsets;

        public FVirtualTextureTileOffsetData(FArchive Ar)
        {
            Width = Ar.Read<uint>();
            Height = Ar.Read<uint>();
            MaxAddress = Ar.Read<uint>();
            Addresses = Ar.ReadArray<uint>();
            Offsets = Ar.ReadArray<uint>();
        }

        public uint GetTileOffset(uint InAddress)
        {
            var BlockIndex = Addresses.FirstOrDefault(x => x >= InAddress); // Algo::UpperBound(Addresses, InAddress) - 1;
            var BaseOffset = Offsets[BlockIndex];
            if (BaseOffset == ~0u)
            {
                return ~0u;
            }

            uint BaseAddress = Addresses[BlockIndex];
            uint LocalOffset = InAddress - BaseAddress;
            return BaseOffset + LocalOffset;
        }
    }

    public class FVirtualTextureBuiltData
    {
        public readonly uint NumLayers;
        public readonly uint? NumMips;
        public readonly uint Width;
        public readonly uint Height;
        public readonly uint WidthInBlocks;
        public readonly uint HeightInBlocks;
        public readonly uint TileSize;
        public readonly uint TileBorderSize;
        public readonly EPixelFormat[] LayerTypes;
        public readonly FVirtualTextureDataChunk[] Chunks;
        public readonly uint[]? TileIndexPerChunk;
        public readonly uint[]? TileIndexPerMip;
        public readonly uint[]? TileOffsetInChunk;
        public readonly uint[]? ChunkIndexPerMip;
        public readonly uint[]? BaseOffsetPerMip;
        public readonly uint[]? TileDataOffsetPerLayer;
        public readonly FVirtualTextureTileOffsetData[] TileOffsetData;
        public readonly FLinearColor[] LayerFallbackColors;

        public FVirtualTextureBuiltData(FAssetArchive Ar, int firstMip)
        {
            bool bStripMips = firstMip > 0;
            var bCooked = Ar.ReadBoolean();

            NumLayers = Ar.Read<uint>();
            Debug.Assert(NumLayers <= 8u); // VIRTUALTEXTURE_DATA_MAXLAYERS
            WidthInBlocks = Ar.Read<uint>();
            HeightInBlocks = Ar.Read<uint>();
            TileSize = Ar.Read<uint>();
            TileBorderSize = Ar.Read<uint>();
            if (Ar.Game >= EGame.GAME_UE5_0) TileDataOffsetPerLayer = Ar.ReadArray<uint>();

            if (!bStripMips)
            {
                NumMips = Ar.Read<uint>();
                Width = Ar.Read<uint>();
                Height = Ar.Read<uint>();

                if (Ar.Game >= EGame.GAME_UE5_0)
                {
                    ChunkIndexPerMip = Ar.ReadArray<uint>();
                    BaseOffsetPerMip = Ar.ReadArray<uint>();
                    TileOffsetData = Ar.ReadArray(() => new FVirtualTextureTileOffsetData(Ar));
                }

                TileIndexPerChunk = Ar.ReadArray<uint>();
                TileIndexPerMip = Ar.ReadArray<uint>();
                TileOffsetInChunk = Ar.ReadArray<uint>();
            }

            LayerTypes = Ar.ReadArray((int) NumLayers, () => (EPixelFormat) Enum.Parse(typeof(EPixelFormat), Ar.ReadFString()));

            if (Ar.Game >= EGame.GAME_UE5_0)
            {
                LayerFallbackColors = new FLinearColor[NumLayers];
                for (int i = 0; i < NumLayers; i++)
                {
                    LayerFallbackColors[i] = Ar.Read<FLinearColor>();
                }
            }

            Chunks = Ar.ReadArray(() => new FVirtualTextureDataChunk(Ar, NumLayers));
        }

        public int GetChunkIndex(int vLevel)
        {
            return ChunkIndexPerMip != null && vLevel < ChunkIndexPerMip.Length ? (int) ChunkIndexPerMip[vLevel] : -1;
        }

        private bool IsLegacyData()
        {
            return TileOffsetInChunk == null || TileOffsetInChunk.Length > 0;
        }

        public uint GetTileOffset(int vLevel, uint vAddress, uint LayerIndex)
        {
            uint offset = 0u;
            if (IsLegacyData())
            {
                throw new NotImplementedException("TODO: Legacy data");
            }
            else
            {
                if (BaseOffsetPerMip != null && BaseOffsetPerMip.Length > vLevel && TileOffsetData.Length > vLevel)
                {
                    // If the tile offset is ~0u there is no data present so we return ~0u to indicate that.
                    var BaseOffset = BaseOffsetPerMip[vLevel];
                    var TileOffset = TileOffsetData[vLevel].GetTileOffset(vAddress);
                    if (BaseOffset != ~0u && TileOffset != ~0u)
                    {
                        Debug.Assert(TileDataOffsetPerLayer != null, nameof(TileDataOffsetPerLayer) + " != null");
                        var TileDataSize = TileDataOffsetPerLayer.Last();
                        var LayerDataOffset = LayerIndex == 0 ? 0 : TileDataOffsetPerLayer[LayerIndex - 1];

                        offset = BaseOffset + (TileOffset * TileDataSize) + LayerDataOffset;
                    }
                }
            }

            return offset;
        }
    }
}
