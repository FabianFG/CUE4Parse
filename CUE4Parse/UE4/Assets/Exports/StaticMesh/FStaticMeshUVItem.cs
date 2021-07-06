using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Objects.RenderCore;
using System.Linq;
using CUE4Parse.UE4.Objects.Meshes;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    [JsonConverter(typeof(FStaticMeshUVItemConverter))]
    public class FStaticMeshUVItem
    {
        public readonly FPackedNormal[] Normal;
        public readonly FMeshUVFloat[] UV;

        public FStaticMeshUVItem(FArchive Ar, bool useHighPrecisionTangents, int numStaticUVSets, bool useStaticFloatUVs)
        {
            Normal = SerializeTangents(Ar, useHighPrecisionTangents);
            UV = SerializeTexcoords(Ar, numStaticUVSets, useStaticFloatUVs);
        }

        public FStaticMeshUVItem(FPackedNormal[] normal, FMeshUVFloat[] uv)
        {
            Normal = normal;
            UV = uv;
        }

        public static FPackedNormal[] SerializeTangents(FArchive Ar, bool useHighPrecisionTangents)
        {
            if (!useHighPrecisionTangents)
                return new [] { new FPackedNormal(Ar), new FPackedNormal(0), new FPackedNormal(Ar) }; // # TangentX and TangentZ

            return new [] { (FPackedNormal)new FPackedRGBA16N(Ar), new FPackedNormal(0), (FPackedNormal)new FPackedRGBA16N(Ar) };
        }

        public static FMeshUVFloat[] SerializeTexcoords(FArchive Ar, int numStaticUVSets, bool useStaticFloatUVs)
        {
            var uv = new FMeshUVFloat[numStaticUVSets];
            if (useStaticFloatUVs)
            {
                for (var i = 0; i < uv.Length; i++)
                {
                    uv[i] = Ar.Read<FMeshUVFloat>();
                }
            }
            else
            {
                for (var i = 0; i < uv.Length; i++)
                {
                    uv[i] = (FMeshUVFloat) Ar.Read<FMeshUVHalf>();
                }
            }
            return uv;
        }
    }
    
    public class FStaticMeshUVItemConverter : JsonConverter<FStaticMeshUVItem>
    {
        public override void WriteJson(JsonWriter writer, FStaticMeshUVItem value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Normal");
            serializer.Serialize(writer, value.Normal);

            writer.WritePropertyName("UV");
            serializer.Serialize(writer, value.UV);

            writer.WriteEndObject();
        }

        public override FStaticMeshUVItem ReadJson(JsonReader reader, Type objectType, FStaticMeshUVItem existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
