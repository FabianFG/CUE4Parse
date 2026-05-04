using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Serilog;
using static CUE4Parse.UE4.Assets.Objects.EBulkDataFlags;

namespace CUE4Parse.UE4.Assets.Objects;

/// <summary>
/// Custom wrapper class for a bulk byte[] data without FByteBulkDataHeader
/// </summary>
public sealed class FByteArrayData : TBulkData<byte>
{
    public FByteArrayData(byte[] data) : base(data) { }

    public FByteArrayData(Lazy<byte[]?> data) : base(data) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetDataSize() => Data!.Length;
}

[JsonConverter(typeof(FByteBulkDataConverter))]
public sealed class FByteBulkData : TBulkData<byte>
{
    public FByteBulkData(FAssetArchive Ar) : base(Ar) { }

    /// <summary>
    /// Creates a new FByteBulkData instance for a portion of the original bulk data.
    /// </summary>
    public FByteBulkData(FAssetArchive Ar, FByteBulkData bulkData, long offset, int size) : base()
    {
        var header = bulkData.Header;
        Header = new FByteBulkDataHeader(header.BulkDataFlags, size, (uint) size, header.OffsetInFile + offset, header.CookedIndex);
        _dataPosition = bulkData._dataPosition;
        if (!header.BulkDataFlags.HasFlag(BULKDATA_OptionalPayload | BULKDATA_PayloadInSeperateFile | BULKDATA_PayloadAtEndOfFile))
        {
            _dataPosition += offset;
        }

        if (Header.SizeOnDisk == 0 || BulkDataFlags.HasFlag(BULKDATA_Unused))
        {
            return;
        }

        _savedAr = Ar;

        _data = new Lazy<byte[]?>(() =>
        {
            return ReadBulkDataInto(out var data) ? data : null;
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetDataSize() => Header.ElementCount;

    protected override bool ReadBulkDataInto(out byte[] data)
    {
        data = [];
        if (!GetBulkArchive(out var archive, out var position))
        {
            return false;
        }

        data = new byte[(int) Header.SizeOnDisk];
        var read = archive.ReadAt(position, data, 0, (int) Header.SizeOnDisk);
        if (read != Header.SizeOnDisk)
        {
            Log.Warning("Read {read} bytes, expected {Header.SizeOnDisk}", read, Header.SizeOnDisk);
        }

        if (BulkDataFlags.HasFlag(BULKDATA_SerializeCompressedZLIB))
        {
            var uncompressedData = new byte[Header.ElementCount];
            using var dataAr = new FByteArchive("", data, _savedAr?.Versions);
            dataAr.SerializeCompressedNew(uncompressedData, GetDataSize(), "Zlib", ECompressionFlags.COMPRESS_NoFlags, false, out _);
            data = uncompressedData;
        }

        return true;
    }

    public bool TryCombineBulkData(FAssetArchive Ar, out byte[] combinedData, out FByteBulkData? fullBulkData)
    {
        fullBulkData = null;
        combinedData = [];
        try
        {
            var saved = Ar.Position;
            var secondChunk = new FByteBulkData(Ar);
            var secondChunkData = secondChunk.Data;
            if (Data is null || secondChunkData is null) return false;

            if (Data.Length < secondChunkData.Length && secondChunkData.AsSpan()[..Data.Length].SequenceEqual(Data))
            {
                combinedData = secondChunkData;
                fullBulkData = secondChunk;
                return true;
            }

            combinedData = new byte[GetDataSize() + secondChunk.GetDataSize()];
            Buffer.BlockCopy(Data, 0, combinedData, 0, GetDataSize());
            Buffer.BlockCopy(secondChunkData, 0, combinedData, GetDataSize(), secondChunk.GetDataSize());
            return true;
        }
        catch
        {
            return false;
        }
    }
}
