using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Exceptions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(FByteBulkDataConverter))]
    public class FByteBulkData
    {
        public readonly FByteBulkDataHeader Header;
        public readonly byte[] Data;

        public FByteBulkData(FAssetArchive Ar)
        {
            Header = new FByteBulkDataHeader(Ar);
            var bulkDataFlags = Header.BulkDataFlags;

            Data = new byte[Header.ElementCount];
            if (Header.ElementCount == 0)
            {
                // Nothing to do here
            }
            else if (EBulkData.BULKDATA_Unused.Check(bulkDataFlags))
            {
                Log.Warning("Bulk with no data");
            }
            else if (EBulkData.BULKDATA_ForceInlinePayload.Check(bulkDataFlags))
            {
#if DEBUG
                Log.Debug($"bulk data in .uexp file (Force Inline Payload) (flags={bulkDataFlags}, pos={Header.OffsetInFile}, size={Header.SizeOnDisk}))");       
#endif
                Ar.Read(Data, 0, Header.ElementCount);
            }
            else if (EBulkData.BULKDATA_OptionalPayload.Check(bulkDataFlags))
            {
#if DEBUG
                Log.Debug($"bulk data in .uptnl file (Optional Payload) (flags={bulkDataFlags}, pos={Header.OffsetInFile}, size={Header.SizeOnDisk}))");       
#endif
                var uptnlAr = Ar.GetPayload(PayloadType.UPTNL);
                uptnlAr.Position = Header.OffsetInFile;
                uptnlAr.Read(Data, 0, Header.ElementCount);
            }
            else if (EBulkData.BULKDATA_PayloadInSeperateFile.Check(bulkDataFlags))
            {
#if DEBUG
                Log.Debug($"bulk data in .ubulk file (Payload In Separate File) (flags={bulkDataFlags}, pos={Header.OffsetInFile}, size={Header.SizeOnDisk}))");       
#endif
                var ubulkAr = Ar.GetPayload(PayloadType.UBULK);
                ubulkAr.Position = Header.OffsetInFile;
                ubulkAr.Read(Data, 0, Header.ElementCount);
            }
            else if (EBulkData.BULKDATA_PayloadAtEndOfFile.Check(bulkDataFlags))
            {
#if DEBUG
                Log.Debug($"bulk data in .uexp file (Payload At End Of File) (flags={bulkDataFlags}, pos={Header.OffsetInFile}, size={Header.SizeOnDisk}))");       
#endif          
                //stored in same file, but at different position
                //save archive position
                var savePos = Ar.Position;
                if (Header.OffsetInFile + Header.ElementCount <= Ar.Length)
                {
                    Ar.Position = Header.OffsetInFile;
                    Ar.Read(Data, 0, Header.ElementCount);
                } else throw new ParserException(Ar, $"Failed to read PayloadAtEndOfFile, {Header.OffsetInFile} is out of range");

                Ar.Position = savePos;
            }
            else if (EBulkData.BULKDATA_CompressedZlib.Check(bulkDataFlags))
            {
                throw new ParserException(Ar, "TODO: CompressedZlib");
            }
        }
    }
    
    public class FByteBulkDataConverter : JsonConverter<FByteBulkData>
    {
        public override void WriteJson(JsonWriter writer, FByteBulkData value, JsonSerializer serializer)
        {
            writer.WritePropertyName("Header");
            serializer.Serialize(writer, value.Header);
        }

        public override FByteBulkData ReadJson(JsonReader reader, Type objectType, FByteBulkData existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}