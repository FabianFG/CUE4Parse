using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    [JsonConverter(typeof(FSkeletalMeshVertexBufferConverter))]
    public class FSkeletalMeshVertexBuffer
    {
        public int NumTexCoords;
        public FVector MeshExtension;
        public FVector MeshOrigin;
        public bool bUseFullPrecisionUVs;
        public bool bExtraBoneInfluences;
        public FGPUVertHalf[] VertsHalf;
        public FGPUVertFloat[] VertsFloat;

        public FSkeletalMeshVertexBuffer()
        {
            VertsHalf = Array.Empty<FGPUVertHalf>();
            VertsFloat = Array.Empty<FGPUVertFloat>();
        }

        public FSkeletalMeshVertexBuffer(FAssetArchive Ar) : this()
        {
            var stripDataFlags = new FStripDataFlags(Ar, (int)UE4Version.VER_UE4_STATIC_SKELETAL_MESH_SERIALIZATION_FIX);

            NumTexCoords = Ar.Read<int>();
            bUseFullPrecisionUVs = Ar.ReadBoolean();

            if (Ar.Ver >= UE4Version.VER_UE4_SUPPORT_GPUSKINNING_8_BONE_INFLUENCES &&
                FSkeletalMeshCustomVersion.Get(Ar) < FSkeletalMeshCustomVersion.Type.UseSeparateSkinWeightBuffer)
            {
                bExtraBoneInfluences = Ar.ReadBoolean();
            }

            MeshExtension = Ar.Read<FVector>();
            MeshOrigin = Ar.Read<FVector>();

            if (!bUseFullPrecisionUVs)
                VertsHalf = Ar.ReadBulkArray(() => new FGPUVertHalf(Ar, NumTexCoords));
            else
                VertsFloat = Ar.ReadBulkArray(() => new FGPUVertFloat(Ar, NumTexCoords));
        }

        public int GetVertexCount()
        {
            if (VertsHalf.Length > 0) return VertsHalf.Length;
            if (VertsFloat.Length > 0) return VertsFloat.Length;
            return 0;
        }
    }

    public class FSkeletalMeshVertexBufferConverter : JsonConverter<FSkeletalMeshVertexBuffer>
    {
        public override void WriteJson(JsonWriter writer, FSkeletalMeshVertexBuffer value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("NumTexCoords");
            writer.WriteValue(value.NumTexCoords);

            writer.WritePropertyName("MeshExtension");
            serializer.Serialize(writer, value.MeshExtension);

            writer.WritePropertyName("MeshOrigin");
            serializer.Serialize(writer, value.MeshOrigin);

            writer.WritePropertyName("bUseFullPrecisionUVs");
            writer.WriteValue(value.bUseFullPrecisionUVs);

            writer.WritePropertyName("bExtraBoneInfluences");
            writer.WriteValue(value.bExtraBoneInfluences);

            writer.WriteEndObject();
        }

        public override FSkeletalMeshVertexBuffer ReadJson(JsonReader reader, Type objectType, FSkeletalMeshVertexBuffer existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
