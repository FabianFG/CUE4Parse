using System;
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
        public readonly FByteBulkDataHeader Header;
        public readonly EBulkDataFlags BulkDataFlags;
        public readonly byte[]? Data;

        public FByteBulkData(FAssetArchive Ar)
        {
            Header = new FByteBulkDataHeader(Ar);
            BulkDataFlags = Header.BulkDataFlags;

            if (Header.ElementCount == 0)
            {
                // Nothing to do here
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_Unused))
            {
                Log.Warning("Bulk with no data");
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_ForceInlinePayload))
            {
#if DEBUG
                Log.Debug($"bulk data in .uexp file (Force Inline Payload) (flags={BulkDataFlags}, pos={Header.OffsetInFile}, size={Header.SizeOnDisk}))");
#endif
                Data = new byte[Header.ElementCount];
                Ar.Read(Data, 0, Header.ElementCount);
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_OptionalPayload))
            {
#if DEBUG
                Log.Debug($"bulk data in .uptnl file (Optional Payload) (flags={BulkDataFlags}, pos={Header.OffsetInFile}, size={Header.SizeOnDisk}))");
#endif
                if (!Ar.TryGetPayload(PayloadType.UPTNL, out var uptnlAr) || uptnlAr == null) return;

                Data = new byte[Header.ElementCount];
                uptnlAr.Position = Header.OffsetInFile;
                uptnlAr.Read(Data, 0, Header.ElementCount);
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_PayloadInSeperateFile))
            {
#if DEBUG
                Log.Debug($"bulk data in .ubulk file (Payload In Separate File) (flags={BulkDataFlags}, pos={Header.OffsetInFile}, size={Header.SizeOnDisk}))");
#endif
                if (!Ar.TryGetPayload(PayloadType.UBULK, out var ubulkAr) || ubulkAr == null) return;

                Data = new byte[Header.ElementCount];
                ubulkAr.Position = Header.OffsetInFile;
                ubulkAr.Read(Data, 0, Header.ElementCount);
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_PayloadAtEndOfFile))
            {
#if DEBUG
                Log.Debug($"bulk data in .uexp file (Payload At End Of File) (flags={BulkDataFlags}, pos={Header.OffsetInFile}, size={Header.SizeOnDisk}))");
#endif
                //stored in same file, but at different position
                //save archive position
                var savePos = Ar.Position;
                if (Header.OffsetInFile + Header.ElementCount <= Ar.Length)
                {
                    Data = new byte[Header.ElementCount];
                    Ar.Position = Header.OffsetInFile;
                    Ar.Read(Data, 0, Header.ElementCount);
                }
                else throw new ParserException(Ar, $"Failed to read PayloadAtEndOfFile, {Header.OffsetInFile} is out of range");

                Ar.Position = savePos;
            }
            else if (BulkDataFlags.HasFlag(BULKDATA_SerializeCompressedZLIB))
            {
                throw new ParserException(Ar, "TODO: CompressedZlib");
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
    }

    public class FByteBulkDataConverter : JsonConverter<FByteBulkData>
    {
        public override void WriteJson(JsonWriter writer, FByteBulkData value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Header);
        }

        public override FByteBulkData ReadJson(JsonReader reader, Type objectType, FByteBulkData existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
