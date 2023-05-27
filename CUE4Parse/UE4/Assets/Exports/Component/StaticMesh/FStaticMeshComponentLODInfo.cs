using System;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.StaticMesh
{
    public struct FPaintedVertex
    {
        public FVector Position;
        public FVector4 Normal;
        public FColor Color;

        public FPaintedVertex(FArchive Ar)
        {
            Position = new FVector(Ar);

            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.IncreaseNormalPrecision)
            {
                var temp = new FPackedNormal(Ar);
                Normal = temp;
            }
            else
            {
                Normal = new FVector4(Ar);
            }

            Color = Ar.Read<FColor>();
        }
    }

    [JsonConverter(typeof(FStaticMeshComponentLODInfoConverter))]
    public class FStaticMeshComponentLODInfo
    {
        private const byte OverrideColorsStripFlag = 1;
        public readonly FGuid MapBuildDataId;
        public readonly FPaintedVertex[] PaintedVertices;
        public readonly FColorVertexBuffer? OverrideVertexColors;

        public FStaticMeshComponentLODInfo(FArchive Ar)
        {
            var stripFlags = new FStripDataFlags(Ar);

            if (!stripFlags.IsDataStrippedForServer())
            {
                MapBuildDataId = Ar.Read<FGuid>();
            }

            if (!stripFlags.IsClassDataStripped(OverrideColorsStripFlag))
            {
                var bLoadVertexColorData = Ar.Read<byte>();

                if (bLoadVertexColorData == 1)
                {
                    OverrideVertexColors = new FColorVertexBuffer(Ar);
                }
            }

            if (!stripFlags.IsEditorDataStripped())
            {
                PaintedVertices = Ar.ReadArray(() => new FPaintedVertex(Ar));
            }

            if (Ar.Game == EGame.GAME_StarWarsJediSurvivor) Ar.Position += 20;
        }
    }

    public class FStaticMeshComponentLODInfoConverter : JsonConverter<FStaticMeshComponentLODInfo>
    {
        public override void WriteJson(JsonWriter writer, FStaticMeshComponentLODInfo value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("MapBuildDataId");
            writer.WriteValue(value.MapBuildDataId.ToString());

            if (value.OverrideVertexColors != null)
            {
                writer.WritePropertyName("OverrideVertexColors");
                serializer.Serialize(writer, value.OverrideVertexColors);
            }

            writer.WriteEndObject();
        }

        public override FStaticMeshComponentLODInfo ReadJson(JsonReader reader, Type objectType, FStaticMeshComponentLODInfo existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
