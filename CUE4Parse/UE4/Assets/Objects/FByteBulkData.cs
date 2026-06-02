using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
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

    /// <summary>
    /// Reads bulk data once without storing it in this instance.
    /// If data is already cached, optionally returns a copy of a cached data.
    /// </summary>
    public byte[]? ReadDataOnce(bool returnCachedData = true)
    {
        if (_data is { IsValueCreated: true })
        {
            var cached = _data.Value;
            if (cached is null) return null;

            return returnCachedData ? cached : (byte[]) cached.Clone();
        }

        return ReadBulkDataInto(out var data) ? data : null;
    }

    public bool TryCreateReader(string name, [NotNullWhen(true)] out FArchive reader, bool useCachedData = true)
    {
        try
        {
            var data = ReadDataOnce(useCachedData) ?? throw new ParserException();
            reader = new FByteArchive(name, data, _savedAr?.Versions);
        }
        catch (Exception e)
        {
            Log.Error(e, "Could not create {0} reader for FByteBulkData", name);
            reader = null!;
        }
        return reader != null;
    }

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
        var saved = Ar.Position;
        try
        {
            var secondChunk = new FByteBulkData(Ar);
            var secondChunkData = secondChunk.ReadDataOnce();
            var data = ReadDataOnce();
            if (data is null || secondChunkData is null) return false;

            if (data.Length < secondChunkData.Length && secondChunkData.AsSpan()[..data.Length].SequenceEqual(data))
            {
                combinedData = secondChunkData;
                fullBulkData = secondChunk;
                return true;
            }

            combinedData = new byte[data.Length + secondChunkData.Length];
            data.CopyTo(combinedData.AsSpan());
            secondChunkData.CopyTo(combinedData.AsSpan(data.Length));
            return true;
        }
        catch
        {
            Ar.Position = saved;
            return false;
        }
    }
}
