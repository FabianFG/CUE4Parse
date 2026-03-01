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
        _data = new Lazy<byte[]>(data);
    }

    public FByteBulkData(Lazy<byte[]?> data)
    {
        _data = data;
    }

    public FByteBulkData(FAssetArchive Ar, FByteBulkDataHeader? header)
    {
        Header = header ?? new FByteBulkDataHeader(Ar);
        if (Header.SizeOnDisk == 0 || BulkDataFlags.HasFlag(BULKDATA_Unused))
        {
            // Log.Warning("Bulk with no data");
            return;
        }

        _dataPosition = Ar.Position;
        _savedAr = Ar;

        if (BulkDataFlags.HasFlag(BULKDATA_ForceInlinePayload) || BulkDataFlags is BULKDATA_LazyLoadable)
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

        if (BulkDataFlags.HasFlag(BULKDATA_ForceInlinePayload) || BulkDataFlags is BULKDATA_LazyLoadable)
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

        if (BulkDataFlags.HasFlag(BULKDATA_Unused | BULKDATA_PayloadInSeperateFile | BULKDATA_PayloadAtEndOfFile))
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
#if DEBUG
            Log.Debug("bulk data in .uexp file (Force Inline Payload) (flags={BulkDataFlags}, pos={HeaderOffsetInFile}, size={HeaderSizeOnDisk}))", BulkDataFlags, Header.OffsetInFile, Header.SizeOnDisk);
#endif
        }
        else if (BulkDataFlags.HasFlag(BULKDATA_OptionalPayload))
        {
#if DEBUG
            Log.Debug("bulk data in {CookedIndex}.uptnl file (Optional Payload) (flags={BulkDataFlags}, pos={HeaderOffsetInFile}, size={HeaderSizeOnDisk}))", Header.CookedIndex, BulkDataFlags, Header.OffsetInFile, Header.SizeOnDisk);
#endif
            if (!TryGetBulkPayload(archive, PayloadType.UPTNL, out var uptnlAr))
                return false;

            archive = uptnlAr;
            position = uptnlAr.Length == Header.ElementCount ? 0 : Header.OffsetInFile;
        }
        else if (BulkDataFlags.HasFlag(BULKDATA_PayloadInSeperateFile))
        {
#if DEBUG
            Log.Debug("bulk data in {CookedIndex}.ubulk file (Payload In Separate File) (flags={BulkDataFlags}, pos={HeaderOffsetInFile}, size={HeaderSizeOnDisk}))", Header.CookedIndex, BulkDataFlags, Header.OffsetInFile, Header.SizeOnDisk);
#endif
            if (!TryGetBulkPayload(archive, PayloadType.UBULK, out var ubulkAr))
                return false;

            archive = ubulkAr;
            position = ubulkAr.Length == Header.ElementCount ? 0 : Header.OffsetInFile;
        }
        else if (BulkDataFlags.HasFlag(BULKDATA_PayloadAtEndOfFile))
        {
#if DEBUG
            Log.Debug("bulk data in .uexp file (Payload At End Of File) (flags={BulkDataFlags}, pos={HeaderOffsetInFile}, size={HeaderSizeOnDisk}))", BulkDataFlags, Header.OffsetInFile, Header.SizeOnDisk);
#endif
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

    public bool TryCombineBulkData(FAssetArchive Ar, out byte[] combinedData, out FByteBulkDataHeader? fullBulkData)
    {
        fullBulkData = null;
        combinedData = [];
        try
        {
            var saved = Ar.Position;
            var secondChunk = new FByteBulkData(Ar);
            if (Data is null || secondChunk.Data is null) return false;

            if (Data.Length < secondChunk.Data.Length && secondChunk.Data.AsSpan()[..Data.Length].SequenceEqual(Data))
            {
                combinedData = secondChunk.Data;
                fullBulkData = new (secondChunk.Header);
                return true;
            }

            combinedData = new byte[GetDataSize() + secondChunk.GetDataSize()];
            Buffer.BlockCopy(Data, 0, combinedData, 0, GetDataSize());
            Buffer.BlockCopy(secondChunk.Data, 0, combinedData, GetDataSize(), secondChunk.GetDataSize());
            return true;
        }
        catch
        {

            return false;
        }
    }
}
