using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Exceptions;
using Newtonsoft.Json;
using Serilog;
using static CUE4Parse.UE4.Assets.Objects.EBulkDataFlags;

namespace CUE4Parse.UE4.Assets.Objects
{
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

        public FByteBulkData(FAssetArchive Ar)
        {
            Header = new FByteBulkDataHeader(Ar);
            if (Header.ElementCount == 0 || BulkDataFlags.HasFlag(BULKDATA_Unused))
            {
                // Log.Warning("Bulk with no data");
                return;
            }

            _dataPosition = Ar.Position;
            _savedAr = Ar;

            if (BulkDataFlags.HasFlag(BULKDATA_ForceInlinePayload))
            {
                Ar.Position += Header.ElementCount;
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_SerializeCompressedZLIB)) // but where is data? inlined or in separate file?
            {
                throw new ParserException(Ar, "TODO: CompressedZlib");
            }

            if (LazyLoad)
            {
                _data = new Lazy<byte[]?>(() =>
                {
                    var data = new byte[Header.ElementCount];
                    return ReadBulkDataInto(data) ? data : null;
                });
            }
            else
            {
                var data = new byte[Header.ElementCount];
                if (ReadBulkDataInto(data)) _data = new Lazy<byte[]?>(() => data);
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

        private void CheckReadSize(int read)
        {
            if (read != Header.ElementCount) {
                Log.Warning("Read {read} bytes, expected {Header.ElementCount}", read, Header.ElementCount);
            }
        }

        public bool ReadBulkDataInto(byte[] data, int offset = 0)
        {
            if (data.Length - offset < Header.ElementCount) {
                Log.Error("Data buffer is too small");
                return false;
            }

            var Ar = (FAssetArchive)_savedAr.Clone(); // TODO: remove and use FArchive.ReadAt
            Ar.Position = _dataPosition;
            if (BulkDataFlags.HasFlag(BULKDATA_ForceInlinePayload))
            {
#if DEBUG
                Log.Debug("bulk data in .uexp file (Force Inline Payload) (flags={BulkDataFlags}, pos={HeaderOffsetInFile}, size={HeaderSizeOnDisk}))", BulkDataFlags, Header.OffsetInFile, Header.SizeOnDisk);
#endif
                CheckReadSize(Ar.Read(data, offset, Header.ElementCount));
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_OptionalPayload))
            {
#if DEBUG
                Log.Debug("bulk data in {CookedIndex}.uptnl file (Optional Payload) (flags={BulkDataFlags}, pos={HeaderOffsetInFile}, size={HeaderSizeOnDisk}))", Header.CookedIndex, BulkDataFlags, Header.OffsetInFile, Header.SizeOnDisk);
#endif
                if (!TryGetBulkPayload(Ar, PayloadType.UPTNL, out var uptnlAr)) return false;

                CheckReadSize(uptnlAr.ReadAt(Header.OffsetInFile, data, offset, Header.ElementCount));
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_PayloadInSeperateFile))
            {
#if DEBUG
                Log.Debug("bulk data in {CookedIndex}.ubulk file (Payload In Separate File) (flags={BulkDataFlags}, pos={HeaderOffsetInFile}, size={HeaderSizeOnDisk}))", Header.CookedIndex, BulkDataFlags, Header.OffsetInFile, Header.SizeOnDisk);
#endif
                if (!TryGetBulkPayload(Ar, PayloadType.UBULK, out var ubulkAr)) return false;

                CheckReadSize(ubulkAr.ReadAt(Header.OffsetInFile, data, offset, Header.ElementCount));;
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_PayloadAtEndOfFile))
            {
#if DEBUG
                Log.Debug("bulk data in .uexp file (Payload At End Of File) (flags={BulkDataFlags}, pos={HeaderOffsetInFile}, size={HeaderSizeOnDisk}))", BulkDataFlags, Header.OffsetInFile, Header.SizeOnDisk);
#endif
                // stored in same file, but at different position
                // save archive position
                if (Header.OffsetInFile + Header.ElementCount <= Ar.Length)
                {
                    CheckReadSize(Ar.ReadAt(Header.OffsetInFile, data, offset, Header.ElementCount));
                }
                else throw new ParserException(Ar, $"Failed to read PayloadAtEndOfFile, {Header.OffsetInFile} is out of range");
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_SerializeCompressedZLIB))
            {
                throw new ParserException(Ar, "TODO: CompressedZlib");
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_LazyLoadable) || BulkDataFlags.HasFlag(BULKDATA_None))
            {
                CheckReadSize(Ar.Read(data, offset, Header.ElementCount));
            }

            Ar.Dispose();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetBulkPayload(FAssetArchive Ar, PayloadType type, [MaybeNullWhen(false)] out FAssetArchive payloadAr)
        {
            payloadAr = null;
            if (Header.CookedIndex.IsDefault)
            {
                Ar.TryGetPayload(type, out payloadAr);
            }
            else if (Ar.Owner?.Provider is IVfsFileProvider vfsFileProvider)
            {
                var path = Path.ChangeExtension(Ar.Name, $"{Header.CookedIndex}.{type.ToString().ToLowerInvariant()}");
                if (vfsFileProvider.TryGetGameFile(path, out var file) && file.TryCreateReader(out var reader))
                {
                    payloadAr = new FAssetArchive(reader, Ar.Owner);
                }
            }
            return payloadAr != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDataSize() => Header.ElementCount;

        public bool TryCombineBulkData(FAssetArchive Ar, out byte[] combinedData)
        {
            combinedData = [];
            try
            {
                var secondChunk = new FByteBulkData(Ar);
                if (Data is null || secondChunk.Data is null) return false;

                if (Data.Length < secondChunk.Data.Length && secondChunk.Data.AsSpan()[..Data.Length].SequenceEqual(Data))
                {
                    combinedData = secondChunk.Data;
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
}
