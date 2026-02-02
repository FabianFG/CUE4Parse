using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public struct FOptTexturePlatformData
{
    public uint ExtData;
    public uint NumMipsInTail;
}

public struct FSharedImage
{
    public readonly int SizeX;
    public readonly int SizeY;
    public readonly int SizeZ;
    public readonly EPixelFormat Format;
    public readonly byte GammaSpace;
    public readonly FTexture2DMipMap Mip;

    public FSharedImage(FAssetArchive Ar)
    {
        SizeX = Ar.Read<int>();
        SizeY = Ar.Read<int>();
        SizeZ = Ar.Read<int>();
        Format = Ar.Read<EPixelFormat>();
        GammaSpace = Ar.Read<byte>();
        var RawData = Ar.ReadArray<byte>((int)Ar.Read<long>());

        var bulkdata = new FByteBulkData(RawData);
        Mip = new FTexture2DMipMap(bulkdata, SizeX, SizeY, this.SizeZ);
    }
}

public class FTexturePlatformData
{
    private const uint BitMask_CubeMap = 1u << 31;
    private const uint BitMask_HasOptData = 1u << 30;
    private const uint BitMask_HasCpuCopy = 1u << 29;
    private const uint BitMask_NumSlices = BitMask_HasOptData - 1u;

    public readonly int SizeX;
    public int SizeY;
    public readonly uint PackedData; // NumSlices: 1 for simple texture, 6 for cubemap - 6 textures are joined into one
    public readonly string PixelFormat;
    public readonly FOptTexturePlatformData OptData;
    public readonly int FirstMipToSerialize;
    public FTexture2DMipMap[] Mips;
    public readonly FVirtualTextureBuiltData? VTData;
    public readonly FSharedImage? CPUCopy;

    public FTexturePlatformData()
    {
        SizeX = 0;
        SizeY = 0;
        PackedData = 0;
        PixelFormat = string.Empty;
        OptData = default;
        FirstMipToSerialize = -1;
        Mips = [];
        VTData = null;
    }

    public FTexturePlatformData(FAssetArchive Ar, UTexture Owner, bool bSerializeMipData = true)
    {
        const long PlaceholderDerivedDataSize = 16;
        if (Ar.Game is >= EGame.GAME_UE5_2)
        {
            if (Ar.ReadFlag() && Ar.Game != EGame.GAME_InfinityNikki) // bUsingDerivedData
                throw new NotImplementedException("FTexturePlatformData deserialization using derived data is not implemented.");
            else
                Ar.Position += PlaceholderDerivedDataSize - 1;
        }
        else if (Ar is { Game: >= EGame.GAME_UE5_0, IsFilterEditorOnly: true })
        {
            Ar.Position += PlaceholderDerivedDataSize;
        }

        if (Ar.Game == EGame.GAME_InfinityNikki) Ar.Position += 4;

        if (Ar.Game == EGame.GAME_PlayerUnknownsBattlegrounds)
        {
            SizeX = Ar.Read<short>();
            SizeY = Ar.Read<short>();
            var data = Ar.ReadBytes(3); // int24
            PackedData = (uint) (data[0] + (data[1] << 8) + (data[2] << 16));
        }
        else
        {
            SizeX = Ar.Read<int>();
            SizeY = Ar.Read<int>();
            PackedData = Ar.Read<uint>();
        }

        PixelFormat = Ar.Game == EGame.GAME_GearsOfWar4 ? Ar.ReadFName().Text : Ar.ReadFString();

        if (Ar.Game == EGame.GAME_DragonQuestXI) Ar.Position += 4;
        if (Ar.Game == EGame.GAME_FinalFantasy7Remake && (PackedData & 0xffff) == 16384)
        {
            var unk0 = Ar.Read<int>();
            var unk1 = Ar.Read<int>();
            var mapNum = Ar.Read<int>();
        }

        if (HasOptData())
        {
            if (Ar.Game == EGame.GAME_MidnightSuns) Ar.Position += 4;
            if (Ar.Game == EGame.GAME_Psychonauts2) Ar.Position += 24;
            OptData = Ar.Read<FOptTexturePlatformData>();
        }

        if (HasCpuCopy()) // 5.4+
        {
            CPUCopy = new FSharedImage(Ar);
        }

        FirstMipToSerialize = Ar.Read<int>(); // only for cooked, but we don't read FTexturePlatformData for non-cooked textures

        var mipCount = Ar.Read<int>();

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

        if (Ar.Game == EGame.GAME_DaysGone) Ar.Position += 8;

        Mips = new FTexture2DMipMap[mipCount];
        for (var i = 0; i < Mips.Length; i++)
        {
            Mips[i] = new FTexture2DMipMap(Ar, bSerializeMipData);

            if (Owner is UVolumeTexture or UTextureCube)
            {
                var slices = GetNumSlices();
                if (Ar.Game == EGame.GAME_Borderlands4) slices = slices != 1 ? slices >> 1 : 1;
                Mips[i].SizeY *= slices;
                Mips[i].SizeZ = Mips[i].SizeZ == slices ? 1 : Mips[i].SizeZ;
            }
        }

        if (Ar.Versions["VirtualTextures"])
        {
            var bIsVirtual = Ar.ReadBoolean();
            if (bIsVirtual)
            {
                var LODBias = Owner.GetOrDefault<int>("LODBias");
                VTData = new FVirtualTextureBuiltData(Ar, FirstMipToSerialize - LODBias);
            }
        }

        if (Ar.Game is EGame.GAME_AssaultFireFuture && Ar.ReadBoolean()) Ar.Position += 112; 

        if (Mips.Length > 0)
        {
            SizeX = Mips[0].SizeX;
            SizeY = Mips[0].SizeY;

            if (Owner is UVolumeTexture)
                PackedData = (uint) ((Mips[0].SizeZ & BitMask_NumSlices) | (PackedData & ~BitMask_NumSlices));
        }
        else if (VTData != null)
        {
            SizeX = (int) VTData.Width;
            SizeY = (int) VTData.Height;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasCpuCopy() => (PackedData & BitMask_HasCpuCopy) == BitMask_HasCpuCopy;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasOptData() => (PackedData & BitMask_HasOptData) == BitMask_HasOptData;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsCubemap() => (PackedData & BitMask_CubeMap) == BitMask_CubeMap;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetNumSlices() => (int) (PackedData & BitMask_NumSlices);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetNumMipsInTail() => (int) OptData.NumMipsInTail;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetExtData() => (int) OptData.ExtData;
}
