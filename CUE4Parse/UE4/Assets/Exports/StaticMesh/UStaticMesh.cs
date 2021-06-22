using System;
using System.IO;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    [JsonConverter(typeof(UStaticMeshConverter))]
    public class UStaticMesh : UObject
    {
        public bool bCooked { get; private set; }
        public FGuid LightingGuid { get; private set; }
        public Lazy<UObject?>[] Sockets { get; private set; }
        public FStaticMeshRenderData RenderData { get; private set; }
        public FStaticMaterial[]? StaticMaterials { get; private set; }
        public Lazy<UMaterialInterface?>[]? Materials { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            FStripDataFlags stripDataFlags = Ar.Read<FStripDataFlags>();

            bool bCooked = Ar.ReadBoolean();

            var bodySetup = Ar.ReadObject<UObject>();

            var navCollision = Ar.Ver >= UE4Version.VER_UE4_STATIC_MESH_STORE_NAV_COLLISION ? Ar.ReadObject<UObject>() : null;

            if (!stripDataFlags.IsEditorDataStripped())
            {
                throw new NotImplementedException("Static Mesh with Editor Data not implemented yet");
                // if (Ar.Ver < UE4Version.VER_UE4_DEPRECATED_STATIC_MESH_THUMBNAIL_PROPERTIES_REMOVED)
                // {
                //     var dummyThumbnailAngle = Ar.Read<FRotator>();
                //     var dummyThumbnailDistance = Ar.Read<float>();
                // }
                // var highResSourceMeshName = Ar.ReadString();
                // var highResSourceMeshCRC = Ar.Read<uint>();
            }

            LightingGuid = Ar.Read<FGuid>(); // LocalLightingGuid
            Sockets = Ar.ReadArray(() => Ar.ReadObject<UObject>());

            RenderData = new FStaticMeshRenderData(Ar, bCooked);

            if (bCooked & Ar.Game >= EGame.GAME_UE4_20)
            {
                bool hasOccluderData = Ar.ReadBoolean();
                if (hasOccluderData)
                    Ar.ReadArray<FVector>(); // Vertices
                    Ar.ReadArray<ushort>();  // Indics
            }

            if (Ar.Game >= EGame.GAME_UE4_14)
            {
                bool hasSpeedTreeWind = Ar.ReadBoolean();
                if (hasSpeedTreeWind)
                {
                    Ar.Seek(validPos, SeekOrigin.Begin);
                    return;
                } else
                {
                    if (FEditorObjectVersion.Get(Ar) >= FEditorObjectVersion.Type.RefactorMeshEditorMaterials)
                    {
                        // UE4.14+ - "Materials" are deprecated, added StaticMaterials
                        StaticMaterials = Ar.ReadArray(() => new FStaticMaterial(Ar));
                    }
                }
            }

            if (StaticMaterials != null && StaticMaterials.Length != 0)
            {
                Materials = new Lazy<UMaterialInterface?>[StaticMaterials.Length];
                for (int i = 0; i < StaticMaterials.Length; i++)
                {
                    Materials[i] = StaticMaterials[i].MaterialInterface;
                }
            }
        }
    }

    public class UStaticMeshConverter : JsonConverter<UStaticMesh>
    {
        public override void WriteJson(JsonWriter writer, UStaticMesh value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            // export type
            writer.WritePropertyName("Type");
            writer.WriteValue(value.ExportType);

            if (!value.Name.Equals(value.ExportType))
            {
                writer.WritePropertyName("Name");
                writer.WriteValue(value.Name);
            }

            writer.WritePropertyName("Properties");
            writer.WriteStartObject();
            {
                writer.WritePropertyName("RenderData");
                serializer.Serialize(writer, value.RenderData);

                writer.WritePropertyName("LightingGuid");
                serializer.Serialize(writer, value.LightingGuid);

                foreach (var property in value.Properties)
                {
                    writer.WritePropertyName(property.Name.Text);
                    serializer.Serialize(writer, property.Tag);
                }

            }
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        public override UStaticMesh ReadJson(JsonReader reader, Type objectType, UStaticMesh existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
