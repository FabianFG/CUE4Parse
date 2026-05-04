using System;
using System.Buffers;
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

[JsonConverter(typeof(TBulkDataConverter))]
public abstract class TBulkData<T> where T: struct
{
    public FByteBulkDataHeader Header { get; init; }
    public EBulkDataFlags BulkDataFlags => Header.BulkDataFlags;

    public T[]? Data => _data?.Value;
    protected Lazy<T[]?>? _data { get; init; }

    protected FAssetArchive? _savedAr { get; init; }
    protected long _dataPosition { get; init; }

    protected TBulkData() { }

    protected TBulkData(T[] data)
    {
        _data = new Lazy<T[]?>(data);
    }

    protected TBulkData(Lazy<T[]?> data)
    {
        _data = data;
    }

    protected TBulkData(FAssetArchive Ar)
    {
        Header = new FByteBulkDataHeader(Ar);
        if (Header.SizeOnDisk == 0 || BulkDataFlags.HasFlag(BULKDATA_Unused))
        {
            _data = new Lazy<T[]?>(() => []);
            return;
        }

        _dataPosition = Ar.Position;
        _savedAr = Ar;

        if (BulkDataFlags.HasFlag(BULKDATA_ForceInlinePayload) || BulkDataFlags is BULKDATA_LazyLoadable or BULKDATA_None)
        {
            Ar.Position += Header.SizeOnDisk;
        }

        _data = new Lazy<T[]?>(() => ReadBulkDataInto(out var data) ? data : null);
    }

    /// <summary>
    /// Returns the size of the bulk data in bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual int GetDataSize() => Header.ElementCount * Unsafe.SizeOf<T>();

    protected virtual bool ReadBulkDataInto(out T[] data)
    {
        data = [];
        if (_savedAr is null)
            return false;

        if (!GetBulkArchive(out var archive, out var position))
        {
            return false;
        }

        var bulkData = ArrayPool<byte>.Shared.Rent((int) Header.SizeOnDisk);
        Array.Clear(bulkData, 0, (int) Header.SizeOnDisk);
        var read = archive.ReadAt(position, bulkData, 0, (int) Header.SizeOnDisk);
        if (read != Header.SizeOnDisk)
        {
            Log.Warning("Read {read} bytes, expected {sizeOnDisk}", read, Header.SizeOnDisk);
        }

        using var dataAr = new FByteArchive("", bulkData, Header.SizeOnDisk, _savedAr.Versions);
        if (BulkDataFlags.HasFlag(BULKDATA_SerializeCompressedZLIB))
        {
            var size = GetDataSize();
            var uncompressedData = new byte[size];
            data = new T[Header.ElementCount];
            dataAr.SerializeCompressedNew(uncompressedData, size, "Zlib", ECompressionFlags.COMPRESS_NoFlags, false, out _);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref data[0]), ref uncompressedData[0], (uint) size);

            // To-Do rewrite once SerializeCompressedNew/Decompress works with span  
            // var dest = MemoryMarshal.AsBytes(data.AsSpan());
            // dataAr.SerializeCompressedNew(dest, size, "Zlib", ECompressionFlags.COMPRESS_NoFlags, false, out _);
        }
        else
        {
            data = dataAr.ReadArray<T>(Header.ElementCount);
        }

        ArrayPool<byte>.Shared.Return(bulkData);
        return true;
    }

    protected bool GetBulkArchive([NotNullWhen(true)] out FAssetArchive? archive, out long position)
    {
        archive = _savedAr;
        position = _dataPosition;
        if (archive is null)
            return false;

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
}
