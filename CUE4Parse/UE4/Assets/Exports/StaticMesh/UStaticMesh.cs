using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    public class UStaticMesh : UObject
    {
        public bool bCooked { get; private set; }
        public FPackageIndex BodySetup { get; private set; }
        public FPackageIndex NavCollision { get; private set; }
        public FGuid LightingGuid { get; private set; }
        public FPackageIndex[] Sockets { get; private set; } // UStaticMeshSocket[]
        public FStaticMeshRenderData? RenderData { get; private set; }
        public FStaticMaterial[]? StaticMaterials { get; private set; }
        public ResolvedObject?[] Materials { get; private set; } // UMaterialInterface[]

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Materials = Array.Empty<ResolvedObject>();

            var stripDataFlags = Ar.Read<FStripDataFlags>();
            bCooked = Ar.ReadBoolean();
            BodySetup = new FPackageIndex(Ar);

            if (Ar.Game != EGame.GAME_GearsOfWar4 && Ar.Ver >= EUnrealEngineObjectUE4Version.STATIC_MESH_STORE_NAV_COLLISION)
                NavCollision = new FPackageIndex(Ar);

            if (!stripDataFlags.IsEditorDataStripped())
            {
                // if (Ar.Ver < EUnrealEngineObjectUE4Version.DEPRECATED_STATIC_MESH_THUMBNAIL_PROPERTIES_REMOVED)
                // {
                //     var dummyThumbnailAngle = new FRotator(Ar);
                //     var dummyThumbnailDistance = Ar.Read<float>();
                // }
                //
                // var highResSourceMeshName = Ar.ReadFString();
                // var highResSourceMeshCRC = Ar.Read<uint>();
            }

            LightingGuid = Ar.Read<FGuid>(); // LocalLightingGuid
            Sockets = Ar.ReadArray(() => new FPackageIndex(Ar));
            RenderData = new FStaticMeshRenderData(Ar, bCooked);

            if (bCooked && Ar.Game is >= EGame.GAME_UE4_20 and < EGame.GAME_UE5_0)
            {
                var bHasOccluderData = Ar.ReadBoolean();
                if (bHasOccluderData)
                {
                    Ar.ReadArray<FVector>(); // Vertices
                    Ar.ReadArray<ushort>();  // Indices
                }
            }

            if (Ar.Game >= EGame.GAME_UE4_14)
            {
                var bHasSpeedTreeWind = Ar.ReadBoolean();
                if (bHasSpeedTreeWind)
                {
                    Ar.Position = validPos;
                    return;
                }

                if (FEditorObjectVersion.Get(Ar) >= FEditorObjectVersion.Type.RefactorMeshEditorMaterials)
                {
                    // UE4.14+ - "Materials" are deprecated, added StaticMaterials
                    StaticMaterials = Ar.ReadArray(() => new FStaticMaterial(Ar));
                    Materials = new ResolvedObject[StaticMaterials.Length];
                    for (var i = 0; i < Materials.Length; i++)
                    {
                        Materials[i] = StaticMaterials[i].MaterialInterface;
                    }
                }
            }
            else if (TryGetValue(out FPackageIndex[] materials, "Materials"))
            {
                Materials = new ResolvedObject[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    Materials[i] = materials[i].ResolvedObject!;
                }
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("BodySetup");
            serializer.Serialize(writer, BodySetup);

            writer.WritePropertyName("NavCollision");
            serializer.Serialize(writer, NavCollision);

            writer.WritePropertyName("LightingGuid");
            serializer.Serialize(writer, LightingGuid);

            writer.WritePropertyName("RenderData");
            serializer.Serialize(writer, RenderData);
        }
    }
}
