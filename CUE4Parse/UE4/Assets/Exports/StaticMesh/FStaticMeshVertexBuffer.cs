using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using System;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    [JsonConverter(typeof(FStaticMeshVertexBufferConverter))]
    public class FStaticMeshVertexBuffer
    {
        public readonly int NumTexCoords;
        public readonly int Strides;
        public readonly int NumVertices;
        public readonly bool UseFullPrecisionUVs;
        public readonly bool UseHighPrecisionTangentBasis;
        public readonly FStaticMeshUVItem[] UV;  // TangentsData ?

        public FStaticMeshVertexBuffer(FAssetArchive Ar)
        {
            var stripDataFlags = new FStripDataFlags(Ar, (int)UE4Version.VER_UE4_STATIC_SKELETAL_MESH_SERIALIZATION_FIX);

            // SerializeMetaData
            NumTexCoords = Ar.Read<int>();
            Strides = Ar.Game < EGame.GAME_UE4_19 ? Ar.Read<int>() : -1;
            NumVertices = Ar.Read<int>();
            UseFullPrecisionUVs = Ar.ReadBoolean();
            UseHighPrecisionTangentBasis = Ar.Game >= EGame.GAME_UE4_12 && Ar.ReadBoolean();

            if (!stripDataFlags.IsDataStrippedForServer())
            {
                if (Ar.Game < EGame.GAME_UE4_19)
                {
                    UV = Ar.ReadBulkArray(() => new FStaticMeshUVItem(Ar, UseHighPrecisionTangentBasis, NumTexCoords, UseFullPrecisionUVs));
                }
                else
                {
                    // BulkSerialize
                    var itemSize = Ar.Read<int>();
                    var itemCount = Ar.Read<int>();
                    var position = Ar.Position;

                    if (itemCount != NumVertices)
                        throw new ParserException($"NumVertices={itemCount} != NumVertices={NumVertices}");
                    
                    var tempTangents = Ar.ReadArray(NumVertices, () => FStaticMeshUVItem.SerializeTangents(Ar, UseHighPrecisionTangentBasis));
                    if (Ar.Position - position != itemCount * itemSize)
                        throw new ParserException($"Read incorrect amount of tangent bytes, at {Ar.Position}, should be: {position + itemSize * itemCount} behind: {position + (itemSize * itemCount) - Ar.Position}");

                    itemSize = Ar.Read<int>();
                    itemCount = Ar.Read<int>();
                    position = Ar.Position;

                    if (itemCount != NumVertices * NumTexCoords)
                        throw new ParserException($"NumVertices={itemCount} != {NumVertices * NumTexCoords}");

                    var uv = Ar.ReadArray(NumVertices, () => FStaticMeshUVItem.SerializeTexcoords(Ar, NumTexCoords, UseFullPrecisionUVs));
                    if (Ar.Position - position != itemCount * itemSize)
                        throw new ParserException($"Read incorrect amount of Texture Coordinate bytes, at {Ar.Position}, should be: {position + itemSize * itemCount} behind: {position + (itemSize * itemCount) - Ar.Position}");

                    UV = new FStaticMeshUVItem[NumVertices];
                    for (var i = 0; i < NumVertices; i++)
                    {
                        UV[i] = new FStaticMeshUVItem(tempTangents[i], uv[i]);
                    }
                }
            }
            else
            {
                UV = new FStaticMeshUVItem[0];
            }
        }
    }

    public class FStaticMeshVertexBufferConverter : JsonConverter<FStaticMeshVertexBuffer>
    {
        public override void WriteJson(JsonWriter writer, FStaticMeshVertexBuffer value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("NumTexCoords");
            writer.WriteValue(value.NumTexCoords);

            writer.WritePropertyName("NumVertices");
            writer.WriteValue(value.NumVertices);
            
            writer.WritePropertyName("Strides");
            writer.WriteValue(value.Strides);

            writer.WritePropertyName("UseHighPrecisionTangentBasis");
            writer.WriteValue(value.UseHighPrecisionTangentBasis);

            writer.WritePropertyName("UseFullPrecisionUVs");
            writer.WriteValue(value.UseFullPrecisionUVs);

            writer.WriteEndObject();
        }

        public override FStaticMeshVertexBuffer ReadJson(JsonReader reader, Type objectType, FStaticMeshVertexBuffer existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
