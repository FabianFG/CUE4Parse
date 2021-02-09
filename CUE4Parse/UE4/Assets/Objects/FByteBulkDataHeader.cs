using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(FByteBulkDataHeaderConverter))]
    public readonly struct FByteBulkDataHeader
    {
        public readonly int BulkDataFlags;
        public readonly int ElementCount;
        public readonly int SizeOnDisk;
        public readonly long OffsetInFile;

        public FByteBulkDataHeader(FAssetArchive Ar)
        {
            BulkDataFlags = Ar.Read<int>();
            if (EBulkData.BULKDATA_Size64Bit.Check(BulkDataFlags))
                throw new ParserException(Ar, "Must not have Size64Bit flag");
            ElementCount = Ar.Read<int>();
            SizeOnDisk = Ar.Read<int>();
            OffsetInFile = Ar.Ver >= UE4Version.VER_UE4_BULKDATA_AT_LARGE_OFFSETS ? Ar.Read<long>() : Ar.Read<int>();
            if (!EBulkData.BULKDATA_NoOffsetFixUp.Check(BulkDataFlags)) // UE4.26 flag
            {
                OffsetInFile += Ar.Owner.Summary.BulkDataStartOffset;
            }

            if (EBulkData.BULKDATA_BadDataVersion.Check(BulkDataFlags))
            {
                Ar.Position += sizeof(ushort);
                BulkDataFlags &= ~(int)EBulkData.BULKDATA_BadDataVersion;
            }
        }
    }
    
    public class FByteBulkDataHeaderConverter : JsonConverter<FByteBulkDataHeader>
    {
        public override void WriteJson(JsonWriter writer, FByteBulkDataHeader value, JsonSerializer serializer)
        {
            writer.WritePropertyName("ElementCount");
            writer.WriteValue(value.ElementCount);
            
            writer.WritePropertyName("SizeOnDisk");
            writer.WriteValue(value.SizeOnDisk);
            
            writer.WritePropertyName("OffsetInFile");
            writer.WriteValue($"0x{value.OffsetInFile:X}");
        }

        public override FByteBulkDataHeader ReadJson(JsonReader reader, Type objectType, FByteBulkDataHeader existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}