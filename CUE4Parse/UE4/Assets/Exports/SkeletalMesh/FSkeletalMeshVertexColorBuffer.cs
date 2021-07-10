using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    [JsonConverter(typeof(FSkeletalMeshVertexColorBufferConverter))]
    public class FSkeletalMeshVertexColorBuffer
    {
        public readonly FColor[] Data;

        public FSkeletalMeshVertexColorBuffer()
        {
            Data = Array.Empty<FColor>();
        }
        
        public FSkeletalMeshVertexColorBuffer(FAssetArchive Ar)
        {
            var stripDataFlags = new FStripDataFlags(Ar, (int)UE4Version.VER_UE4_STATIC_SKELETAL_MESH_SERIALIZATION_FIX);

            if (!stripDataFlags.IsDataStrippedForServer())
            {
                Data = Ar.ReadBulkArray<FColor>();
            }
            else
            {
                Data = new FColor[0];
            }
        }

        public FSkeletalMeshVertexColorBuffer(FColor[] data)
        {
            Data = data;
        }
    }
    
    public class FSkeletalMeshVertexColorBufferConverter : JsonConverter<FSkeletalMeshVertexColorBuffer>
    {
        public override void WriteJson(JsonWriter writer, FSkeletalMeshVertexColorBuffer value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Data);
        }

        public override FSkeletalMeshVertexColorBuffer ReadJson(JsonReader reader, Type objectType, FSkeletalMeshVertexColorBuffer existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}