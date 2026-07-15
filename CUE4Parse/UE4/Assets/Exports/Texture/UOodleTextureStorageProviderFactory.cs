using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class UOodleTextureStorageProviderFactory : UTextureAllMipDataProviderFactory
{
    public int SizeX { get; private set; }
    public int SizeY { get; private set; }
    public int Version { get; private set; }
    public uint OodleFlags { get; private set; }
    public int[] ModeCounts { get; private set; } = [];

    public uint BulkDataFlags { get; private set; }
    public int ElementCount { get; private set; }
    public int SizeOnDisk { get; private set; }
    public long OffsetInFile { get; private set; }

    [JsonIgnore]
    public byte[] CompressedData { get; private set; } = [];

    private const int SizeXOffset = 20;
    private const int SizeYOffset = 24;
    private const int VersionOffset = 36;
    private const int FlagsOffset = 40;
    private const int ModeCountsOffset = 44;
    private const int ModeCountCount = 10;
    private const int BulkFlagsOffset = 84;
    private const int PayloadOffset = 104;
    private const int TrailerSize = 12;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Game != EGame.GAME_WutheringWaves)
        {
            Ar.Position = validPos;
            return;
        }

        var remaining = (int) (validPos - Ar.Position);
        if (remaining < PayloadOffset + TrailerSize)
        {
            Ar.Position = validPos;
            return;
        }

        var buf = Ar.ReadBytes(remaining);

        int I32(int o) => System.BitConverter.ToInt32(buf, o);
        uint U32(int o) => System.BitConverter.ToUInt32(buf, o);

        SizeX = I32(SizeXOffset);
        SizeY = I32(SizeYOffset);
        Version = I32(VersionOffset);
        OodleFlags = U32(FlagsOffset);

        ModeCounts = new int[ModeCountCount];
        for (var i = 0; i < ModeCounts.Length; i++)
            ModeCounts[i] = I32(ModeCountsOffset + i * 4);

        int bulkHeaderOffset = ModeCountsOffset + ModeCountCount * 4;
        Ar.Position = Ar.Position - buf.Length + bulkHeaderOffset;

        var bulkData = new FByteBulkData(Ar);
        CompressedData = bulkData.Data;
    }

    public byte[] DecodeTopMip()
    {
        if (CompressedData.Length == 0 || SizeX <= 0 || SizeY <= 0)
            throw new ParserException("Oodle texture storage provider has no BC7Prep payload");

        var topMipSize = GetBc7MipSize(SizeX, SizeY);
        var decoded = BC7PrepDecoder.Decode(CompressedData, ModeCounts, OodleFlags, SizeX, SizeY);
        if (decoded == null || decoded.Length != topMipSize)
            throw new ParserException("BC7Prep decode failed");
        return decoded;
    }

    public bool TryCreatePlatformData(out FTexturePlatformData platformData, out EPixelFormat format)
    {
        platformData = new FTexturePlatformData();
        format = EPixelFormat.PF_Unknown;

        if (CompressedData.Length == 0 || SizeX <= 0 || SizeY <= 0)
            return false;

        platformData = new FTexturePlatformData(SizeX, SizeY, EPixelFormat.PF_BC7, new Lazy<byte[]?>(DecodeTopMip));
        format = EPixelFormat.PF_BC7;
        return true;
    }

    public bool TryGetMipData(int mipLevel, out byte[]? data, out int sizeX, out int sizeY, out int sizeZ)
    {
        data = null;
        sizeX = SizeX;
        sizeY = SizeY;
        sizeZ = 1;

        if (mipLevel != 0 || CompressedData.Length == 0 || SizeX <= 0 || SizeY <= 0)
            return false;

        try
        {
            data = DecodeTopMip();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int GetBc7MipSize(int sizeX, int sizeY)
    {
        var blocksX = (sizeX + 3) / 4;
        var blocksY = (sizeY + 3) / 4;
        return blocksX * blocksY * 16;
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName("SizeX");
        writer.WriteValue(SizeX);
        writer.WritePropertyName("SizeY");
        writer.WriteValue(SizeY);
        writer.WritePropertyName("OodleFlags");
        writer.WriteValue($"0x{OodleFlags:X}");
        writer.WritePropertyName("SizeOnDisk");
        writer.WriteValue(SizeOnDisk);
        writer.WritePropertyName("ModeCounts");
        serializer.Serialize(writer, ModeCounts);
    }
}
