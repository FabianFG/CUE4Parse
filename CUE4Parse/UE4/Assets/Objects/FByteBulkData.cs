using System;
using System.Runtime.CompilerServices;
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
        public readonly EBulkDataFlags BulkDataFlags;

        public byte[]? Data => _data?.Value;
        private readonly Lazy<byte[]?>? _data;

        private readonly FAssetArchive _savedAr;
        private readonly long _dataPosition;

        public FByteBulkData(FAssetArchive Ar)
        {
            Header = new FByteBulkDataHeader(Ar);
            BulkDataFlags = Header.BulkDataFlags;

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
                    if (ReadBulkDataInto(data))
                        return data;
                    return null;
                });
            }
            else {
                var data = new byte[Header.ElementCount];
                if (!ReadBulkDataInto(data))
                    throw new ParserException(Ar, "Failed to read bulk data");
                _data = new Lazy<byte[]?>(() => data);
            }
        }

        protected FByteBulkData(FAssetArchive Ar, bool skip = false)
        {
            Header = new FByteBulkDataHeader(Ar);
            var bulkDataFlags = Header.BulkDataFlags;

            if (bulkDataFlags.HasFlag(BULKDATA_Unused | BULKDATA_PayloadInSeperateFile | BULKDATA_PayloadAtEndOfFile))
            {
                return;
            }

            if (bulkDataFlags.HasFlag(BULKDATA_ForceInlinePayload) || Header.OffsetInFile == Ar.Position)
            {
                Ar.Position += Header.SizeOnDisk;
            }
        }

        private void CheckReadSize(int read) {
            if (read != Header.ElementCount) {
                Log.Warning("Read {read} bytes, expected {Header.ElementCount}", read, Header.ElementCount);
            }
        }

        public bool ReadBulkDataInto(byte[] data, int offset = 0) {
            if (data.Length - offset < Header.ElementCount) {
                Log.Error("Data buffer is too small");
                return false;
            }

            var Ar = (FAssetArchive)_savedAr.Clone();
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
                Log.Debug("bulk data in .uptnl file (Optional Payload) (flags={BulkDataFlags}, pos={HeaderOffsetInFile}, size={HeaderSizeOnDisk}))", BulkDataFlags, Header.OffsetInFile, Header.SizeOnDisk);
#endif
                if (!Ar.TryGetPayload(PayloadType.UPTNL, out var uptnlAr) || uptnlAr == null) return false;

                uptnlAr.Position = Header.OffsetInFile;
                CheckReadSize(uptnlAr.Read(data, offset, Header.ElementCount));
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_PayloadInSeperateFile))
            {
#if DEBUG
                Log.Debug("bulk data in .ubulk file (Payload In Separate File) (flags={BulkDataFlags}, pos={HeaderOffsetInFile}, size={HeaderSizeOnDisk}))", BulkDataFlags, Header.OffsetInFile, Header.SizeOnDisk);
#endif
                if (!Ar.TryGetPayload(PayloadType.UBULK, out var ubulkAr) || ubulkAr == null) return false;

                ubulkAr.Position = Header.OffsetInFile;
                CheckReadSize(ubulkAr.Read(data, offset, Header.ElementCount));;
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
                    Ar.Position = Header.OffsetInFile;
                    CheckReadSize(Ar.Read(data, offset, Header.ElementCount));
                }
                else throw new ParserException(Ar, $"Failed to read PayloadAtEndOfFile, {Header.OffsetInFile} is out of range");
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_SerializeCompressedZLIB))
            {
                throw new ParserException(Ar, "TODO: CompressedZlib");
            }
            Ar.Dispose();
            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDataSize() => Header.ElementCount;
    }
}