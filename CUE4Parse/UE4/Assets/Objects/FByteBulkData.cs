using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Serilog;
using static CUE4Parse.UE4.Assets.Objects.EBulkDataFlags;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(FByteBulkDataConverter))]
public class FByteBulkData
{
    public static bool LazyLoad = true;

    public readonly FByteBulkDataHeader Header;
    public EBulkDataFlags BulkDataFlags => Header.BulkDataFlags;

    public byte[]? Data => _data?.Value;
    private readonly Lazy<byte[]?>? _data;

    private readonly FAssetArchive _savedAr;
    private readonly long _dataPosition;

    public FByteBulkData(byte[] data)
    {
        _data = new Lazy<byte[]?>(data);
    }

    public FByteBulkData(Lazy<byte[]?> data)
    {
        _data = data;
    }

    /// <summary>
    /// Creates a new FByteBulkData instance for a portion of the original bulk data.
    /// </summary>
    public FByteBulkData(FAssetArchive Ar, FByteBulkData bulkData, long offset, int size)
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

        if (LazyLoad)
        {
            _data = new Lazy<byte[]?>(() =>
            {
                var data = new byte[Header.SizeOnDisk];
                return ReadBulkDataInto(data) ? data : null;
            });
        }
        else
        {
            var data = new byte[Header.SizeOnDisk];
            if (ReadBulkDataInto(data)) _data = new Lazy<byte[]?>(() => data);
        }
    }

    public FByteBulkData(FAssetArchive Ar)
    {
        Header = new FByteBulkDataHeader(Ar);
        if (Header.SizeOnDisk == 0 || BulkDataFlags.HasFlag(BULKDATA_Unused))
        {
            // Log.Warning("Bulk with no data");
            return;
        }

        _dataPosition = Ar.Position;
        _savedAr = Ar;

        if (BulkDataFlags.HasFlag(BULKDATA_ForceInlinePayload) || BulkDataFlags is BULKDATA_LazyLoadable or BULKDATA_None)
        {
            Ar.Position += Header.SizeOnDisk;
        }

        if (LazyLoad)
        {
            _data = new Lazy<byte[]?>(() =>
            {
                var data = new byte[Header.SizeOnDisk];
                return ReadBulkDataInto(data) ? data : null;
            });
        }
        else
        {
            var data = new byte[Header.SizeOnDisk];
            if (ReadBulkDataInto(data))
                _data = new Lazy<byte[]?>(() => data);
        }
    }

    protected FByteBulkData(FAssetArchive Ar, bool skip = false)
    {
        Header = new FByteBulkDataHeader(Ar);

        if (BulkDataFlags.HasFlag(BULKDATA_Unused | BULKDATA_OptionalPayload |  BULKDATA_PayloadInSeperateFile | BULKDATA_PayloadAtEndOfFile))
        {
            return;
        }

        if (BulkDataFlags.HasFlag(BULKDATA_ForceInlinePayload) || Header.OffsetInFile == Ar.Position)
        {
            Ar.Position += Header.SizeOnDisk;
        }
    }

    private bool ReadBulkDataInto(byte[] data, int offset = 0)
    {
        if (data.Length - offset < Header.SizeOnDisk)
        {
            Log.Error("Data buffer is too small");
            return false;
        }

        var archive = _savedAr;
        var position = _dataPosition;

        if (BulkDataFlags.HasFlag(BULKDATA_ForceInlinePayload))
        {
        }
        else if (BulkDataFlags.HasFlag(BULKDATA_OptionalPayload))
        {

            if (!TryGetBulkPayload(archive, PayloadType.UPTNL, out var uptnlAr))
            {
#if DEBUG
                Log.Debug("Failed to load bulk data in {CookedIndex}.uptnl file (Optional Payload) (flags={BulkDataFlags}, pos={HeaderOffsetInFile}, size={HeaderSizeOnDisk}))", Header.CookedIndex, BulkDataFlags, Header.OffsetInFile, Header.SizeOnDisk);
#endif
                return false;
            }
            

            archive = uptnlAr;
            position = uptnlAr.Length == Header.SizeOnDisk ? 0 : Header.OffsetInFile;
        }
        else if (BulkDataFlags.HasFlag(BULKDATA_PayloadInSeperateFile))
        {
            if (!TryGetBulkPayload(archive, PayloadType.UBULK, out var ubulkAr))
            {
#if DEBUG
                Log.Debug("Failed to load bulk data in {CookedIndex}.ubulk file (Payload In Separate File) (flags={BulkDataFlags}, pos={HeaderOffsetInFile}, size={HeaderSizeOnDisk}))", Header.CookedIndex, BulkDataFlags, Header.OffsetInFile, Header.SizeOnDisk);
#endif
                return false;
            }
            

            archive = ubulkAr;
            position = ubulkAr.Length == Header.SizeOnDisk ? 0 : Header.OffsetInFile;
        }
        else if (BulkDataFlags.HasFlag(BULKDATA_PayloadAtEndOfFile))
        {
            if (Header.OffsetInFile + Header.SizeOnDisk > archive.Length)
                throw new ParserException(archive, $"Failed to read PayloadAtEndOfFile, {Header.OffsetInFile} is out of range");

            // stored in same file, but at different position
            // save archive position
            position = Header.OffsetInFile;
        }
        else if (BulkDataFlags.HasFlag(BULKDATA_LazyLoadable) || BulkDataFlags.HasFlag(BULKDATA_None))
        {
            //
        }

        var read = archive.ReadAt(position, data, offset, (int)Header.SizeOnDisk);
        if (read != Header.SizeOnDisk)
        {
            Log.Warning("Read {read} bytes, expected {Header.SizeOnDisk}", read, Header.SizeOnDisk);
            // return false; // should we???
        }

        if (BulkDataFlags.HasFlag(BULKDATA_SerializeCompressedZLIB))
        {
            var uncompressedData = new byte[Header.ElementCount];
            using var dataAr = new FByteArchive("", data, _savedAr.Versions);
            dataAr.SerializeCompressedNew(uncompressedData, GetDataSize(), "Zlib", ECompressionFlags.COMPRESS_NoFlags, false, out _);
            data = uncompressedData;
            return true;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetBulkPayload(FAssetArchive Ar, PayloadType type, [MaybeNullWhen(false)] out FAssetArchive payloadAr)
    {
        payloadAr = null;
        if (Header.CookedIndex.IsDefault)
        {
            Ar.TryGetPayload(type, out payloadAr, Header);
        }
        else if (Ar.Owner?.Provider is IVfsFileProvider vfsFileProvider)
        {
            var path = Path.ChangeExtension(Ar.Name, $"{Header.CookedIndex}.{type.ToString().ToLowerInvariant()}");
            if (vfsFileProvider.TryGetGameFile(path, out var file) && file.TryCreateReader(out var reader, Header))
            {
                payloadAr = new FAssetArchive(reader, Ar.Owner);
            }
        }
        return payloadAr != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetDataSize() => Header.ElementCount;

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
