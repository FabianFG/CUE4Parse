using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public readonly struct FVirtualTextureTileOffsetData : IUStruct
{
    public readonly uint Width;
    public readonly uint Height;
    public readonly uint MaxAddress;
    public readonly uint[] Addresses;
    public readonly uint[] Offsets;

    public FVirtualTextureTileOffsetData(FArchive Ar)
    {
        Width = Ar.Read<uint>();
        Height = Ar.Read<uint>();
        MaxAddress = Ar.Read<uint>();
        Addresses = Ar.ReadArray<uint>();
        Offsets = Ar.ReadArray<uint>();
    }

    public FVirtualTextureTileOffsetData(uint width, uint height, uint maxAddress)
    {
        Width = width;
        Height = height;
        MaxAddress = maxAddress;
        Addresses = [];
        Offsets = [];
    }

    public uint GetTileOffset(uint inAddress)
    {
        // var blockIndex = Addresses.FirstOrDefault(x => x >= inAddress); // Algo::UpperBound(Addresses, InAddress) - 1;
        var blockIndex = 0;
        if (Addresses != null)
        {
            for (var i = 0; i < Addresses.Length; i++)
            {
                if (Addresses[i] > inAddress)
                {
                    blockIndex = i - 1;
                    break;
                }
                if (i == Addresses.Length - 1 && blockIndex == 0)
                {
                    blockIndex = Addresses.Length - 1;
                }
            }
        }

        var baseOffset = Offsets[blockIndex];
        if (baseOffset == ~0u)
        {
            return ~0u;
        }

        uint baseAddress = Addresses[blockIndex];
        uint localOffset = inAddress - baseAddress;
        return baseOffset + localOffset;
    }
}

public class FVirtualTextureBuiltData
{
    public readonly uint NumLayers;
    public readonly uint NumMips;
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
    public readonly FVirtualTextureTileOffsetData[]? TileOffsetData;
    public readonly FLinearColor[] LayerFallbackColors;

    public FVirtualTextureBuiltData(FAssetArchive Ar, int firstMip)
    {
        //var bStripMips = firstMip > 0 && Ar.Game != EGame.GAME_NobodyWantsToDie;
        var bCooked = Ar.ReadBoolean();

        NumLayers = Ar.Read<uint>();
        Debug.Assert(NumLayers <= 8u); // VIRTUALTEXTURE_DATA_MAXLAYERS
        WidthInBlocks = Ar.Read<uint>();
        HeightInBlocks = Ar.Read<uint>();
        TileSize = Ar.Read<uint>();
        TileBorderSize = Ar.Read<uint>();
        if (Ar.Game >= EGame.GAME_UE5_0) TileDataOffsetPerLayer = Ar.ReadArray<uint>();

        //if (!bStripMips)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInitialized() => TileSize != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetPhysicalTileSize() => TileSize + TileBorderSize * 2u;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetWidthInTiles() => Width.DivideAndRoundUp(TileSize);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetHeightInTiles() => Height.DivideAndRoundUp(TileSize);

    public bool IsLegacyData() => TileOffsetInChunk == null || TileOffsetInChunk.Length > 0;
    public int GetNumTileHeaders() => TileOffsetInChunk?.Length ?? 0;

    public int GetChunkIndex(int vLevel)
        => ChunkIndexPerMip != null && vLevel < ChunkIndexPerMip.Length ? (int) ChunkIndexPerMip[vLevel] : -1;

    public int GetChunkIndex_Legacy(uint tileIndex)
    {
        var max = Chunks.Length - 1;
        if (TileIndexPerChunk != null && tileIndex <= TileIndexPerChunk.Last())
        {
            for (var i = 0; i < max; i++)
            {
                if (tileIndex >= TileIndexPerChunk[i] && tileIndex < TileIndexPerChunk[i + 1])
                    return i;
            }
        }

        return max;
    }

    public uint GetTileIndex_Legacy(int vLevel, uint vAddress)
    {
        Debug.Assert(vLevel < NumMips);
        var tileIndex = TileIndexPerMip[vLevel] + vAddress * NumLayers;
        if (tileIndex >= TileIndexPerMip[vLevel + 1])
        {
            // vAddress is out of bounds for this texture/mip level
            return ~0u;
        }
        return tileIndex;
    }

    public uint GetTileOffset_Legacy(int chunkIndex, uint tileIndex)
    {
        Debug.Assert(tileIndex >= TileIndexPerChunk[chunkIndex]);
        if (tileIndex < TileIndexPerChunk[chunkIndex + 1])
        {
            return TileOffsetInChunk[tileIndex];
        }

        // If TileIndex is past the end of chunk, return the size of chunk
        // This allows us to determine size of region by asking for start/end offsets
        return Chunks[chunkIndex].SizeInBytes;
    }

    public bool IsValidAddress(int vLevel, uint vAddress)
    {
        bool bIsValid = false;
        if (IsLegacyData())
        {
            bIsValid = GetTileIndex_Legacy(vLevel, vAddress) != ~0u;
        }
        else
        {
            if (TileOffsetData != null && vLevel < TileOffsetData.Length)
            {
                var x = MathUtils.ReverseMortonCode2(vAddress);
                var y = MathUtils.ReverseMortonCode2(vAddress >> 1);
                bIsValid = x < TileOffsetData[vLevel].Width && y < TileOffsetData[vLevel].Height;
            }
        }

        return bIsValid;
    }

    public (int, uint, uint) GetTileData(int vLevel, uint vAddress, uint layerIndex)
    {
        int chunkIndex = 0;
        uint offset = 0u;
        uint tileDataLength = 0;

        if (IsLegacyData())
        {
            var tileIndex = GetTileIndex_Legacy(vLevel, vAddress);
            if (tileIndex != ~0u)
            {
                // If size of the tile is 0 we return ~0u to indicate that there is no data present.
                chunkIndex = GetChunkIndex_Legacy(tileIndex);
                var tileOffset = GetTileOffset_Legacy(chunkIndex, tileIndex);
                var nextTileOffset = GetTileOffset_Legacy(chunkIndex, tileIndex + NumLayers);
                if (tileOffset != nextTileOffset)
                {
                    offset = GetTileOffset_Legacy(chunkIndex, tileIndex + layerIndex);
                    tileDataLength = GetTileOffset_Legacy(chunkIndex, tileIndex + layerIndex + 1) - offset;
                }
            }
        }
        else if (BaseOffsetPerMip != null && BaseOffsetPerMip.Length > vLevel && TileOffsetData.Length > vLevel)
        {
            // If the tile offset is ~0u there is no data present so we return ~0u to indicate that.
            chunkIndex = GetChunkIndex(vLevel);
            var baseOffset = BaseOffsetPerMip[vLevel];
            var tileOffset = TileOffsetData[vLevel].GetTileOffset(vAddress);
            if (baseOffset != ~0u && tileOffset != ~0u)
            {
                Debug.Assert(TileDataOffsetPerLayer != null, nameof(TileDataOffsetPerLayer) + " != null");
                var tileDataSize = TileDataOffsetPerLayer.Last();
                tileDataLength = layerIndex == 0 ? 0 : TileDataOffsetPerLayer[layerIndex - 1];

                offset = baseOffset + (tileOffset * tileDataSize) + tileDataLength;
            }
        }

        return (chunkIndex, offset, tileDataLength);
    }
}
