using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    // [JsonConverter(typeof(FSkinWeightInfoConverter))]
    public class FSkinWeightInfo
    {
        private const int _NUM_INFLUENCES_UE4 = 4;
        private const int _MAX_TOTAL_INFLUENCES_UE4 = 8;
        
        public readonly byte[] BoneIndex;
        public readonly byte[] BoneWeight;

        public FSkinWeightInfo()
        {
            BoneIndex = new byte[_NUM_INFLUENCES_UE4];
            BoneWeight = new byte[_NUM_INFLUENCES_UE4];
        }
        
        public FSkinWeightInfo(FArchive Ar, bool numSkelCondition) : this()
        {
            var numSkelInfluences = numSkelCondition ? _MAX_TOTAL_INFLUENCES_UE4 : _NUM_INFLUENCES_UE4;
            if (numSkelInfluences <= BoneIndex.Length)
            {
                for (var i = 0; i < numSkelInfluences; i++)
                    BoneIndex[i] = Ar.Read<byte>();
                for (var i = 0; i < numSkelInfluences; i++)
                    BoneWeight[i] = Ar.Read<byte>();
            }
            else
            {
                var boneIndex2 = new byte[_NUM_INFLUENCES_UE4];
                var boneWeight2 = new byte[_NUM_INFLUENCES_UE4];
                for (var i = 0; i < numSkelInfluences; i++)
                    boneIndex2[i] = Ar.Read<byte>();
                for (var i = 0; i < numSkelInfluences; i++)
                    boneWeight2[i] = Ar.Read<byte>();
                
                // copy influences to vertex
                for (var i = 0; i < _NUM_INFLUENCES_UE4; i++)
                {
                    BoneIndex[i] = boneIndex2[i];
                    BoneWeight[i] = boneWeight2[i];
                }
            }
        }
        
        public int ConvertToInt(byte[] value) => BitConverter.ToInt32(value, 0);
        public long ConvertToLong(byte[] value) => BitConverter.ToInt64(value, 0);
    }
    
    public class FSkinWeightInfoConverter : JsonConverter<FSkinWeightInfo>
    {
        public override void WriteJson(JsonWriter writer, FSkinWeightInfo value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("BoneIndex");
            switch (value.BoneIndex.Length)
            {
                case 4:
                    writer.WriteValue(value.ConvertToInt(value.BoneIndex));
                    break;
                case 8:
                    writer.WriteValue(value.ConvertToLong(value.BoneIndex));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            writer.WritePropertyName("BoneWeight");
            switch (value.BoneWeight.Length)
            {
                case 4:
                    writer.WriteValue(value.ConvertToInt(value.BoneWeight));
                    break;
                case 8:
                    writer.WriteValue(value.ConvertToLong(value.BoneWeight));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            writer.WriteEndObject();
        }

        public override FSkinWeightInfo ReadJson(JsonReader reader, Type objectType, FSkinWeightInfo existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}