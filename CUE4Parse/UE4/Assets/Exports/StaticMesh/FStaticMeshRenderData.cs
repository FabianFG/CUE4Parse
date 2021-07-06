using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    [JsonConverter(typeof(FStaticMeshRenderDataConverter))]
    public class FStaticMeshRenderData
    {
        private const int MAX_STATIC_UV_SETS_UE4 = 8;
        private const int MAX_STATIC_LODS_UE4 = 8;
        
        public readonly FStaticMeshLODResources[] LODs;
        public readonly FBoxSphereBounds Bounds;
        public readonly bool? LODsShareStaticLighting;
        public readonly float[]? ScreenSize;

        public FStaticMeshRenderData(FAssetArchive Ar, bool bCooked)
        {
            if (!bCooked) return;
            
            var minMobileLODIdx = Ar.Game >= EGame.GAME_UE4_27 ? Ar.Read<int>() : 0;
            LODs = Ar.ReadArray(() => new FStaticMeshLODResources(Ar));
            var numInlinedLODs = Ar.Game >= EGame.GAME_UE4_23 ? Ar.ReadByte() : -1;
            
            if (Ar.Ver >= UE4Version.VER_UE4_RENAME_CROUCHMOVESCHARACTERDOWN)
            {
                var stripped = false;
                if (Ar.Ver >= UE4Version.VER_UE4_RENAME_WIDGET_VISIBILITY)
                {
                    var stripDataFlags = Ar.Read<FStripDataFlags>();
                    stripped = stripDataFlags.IsDataStrippedForServer();
                    if (Ar.Game >= EGame.GAME_UE4_21)
                    {
                        stripped |= stripDataFlags.IsClassDataStripped(0x01);
                    }
                }
                    
                if (!stripped)
                {
                    for (var i = 0; i < LODs.Length; i++)
                    {
                        var bValid = Ar.ReadBoolean();
                        if (bValid)
                        {
                            var _ = new FDistanceFieldVolumeData(Ar);
                        }
                    }
                }
            }

            // FortniteGame/Plugins/GameFeatures/Skyfire/Content/Cosmetics/Effects/Prop_Materialize/SM_FX_Skyfire_Backpack.uasset
            // Bounds doesn't seem correct and LODsShareStaticLighting will crash afterward
            Bounds = Ar.Read<FBoxSphereBounds>();

            if (Ar.Game != EGame.GAME_UE4_15)
                LODsShareStaticLighting = Ar.ReadBoolean();

            if (Ar.Game < EGame.GAME_UE4_14)
                Ar.Position += 4; // bReducedBySimplygon

            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.TextureStreamingMeshUVChannelData)
            {
                Ar.Position += 4 * MAX_STATIC_UV_SETS_UE4; // StreamingTextureFactor for each UV set
                Ar.Position += 4;  // MaxStreamingTextureFactor
            }

            ScreenSize = new float[Ar.Game >= EGame.GAME_UE4_9 ? MAX_STATIC_LODS_UE4 : 4];
            for (var i = 0; i < ScreenSize.Length; i++)
            {
                if (Ar.Game >= EGame.GAME_UE4_20)
                    Ar.Position += 4; // bFloatCooked
                    
                ScreenSize[i] = Ar.Read<float>();
            }
        }
    }

    public class FStaticMeshRenderDataConverter : JsonConverter<FStaticMeshRenderData>
    {
        public override void WriteJson(JsonWriter writer, FStaticMeshRenderData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("LODs");
            serializer.Serialize(writer, value.LODs);
            
            writer.WritePropertyName("Bounds");
            serializer.Serialize(writer, value.Bounds);
            
            writer.WritePropertyName("LODsShareStaticLighting");
            writer.WriteValue(value.LODsShareStaticLighting);
            
            writer.WritePropertyName("ScreenSize");
            serializer.Serialize(writer, value.ScreenSize);

            writer.WriteEndObject();
        }

        public override FStaticMeshRenderData ReadJson(JsonReader reader, Type objectType, FStaticMeshRenderData existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
