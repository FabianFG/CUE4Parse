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
        public readonly FStaticMeshLODResources[] LODs;
        public readonly FBoxSphereBounds Bounds;
        public readonly bool? LODsShareStaticLighting;
        public readonly float[]? ScreenSize;

        public FStaticMeshRenderData(FAssetArchive Ar, bool bCooked)
        {
            const int MAX_STATIC_UV_SETS_UE4 = 8;
            const int MAX_STATIC_LODS_UE4 = 8;

            int MinMobileLODIdx = Ar.Game >= EGame.GAME_UE4_27 ? Ar.Read<int>() : 0;
            LODs = Ar.ReadArray(() => new FStaticMeshLODResources(Ar));
            int NumInlinedLODs = Ar.Game >= EGame.GAME_UE4_23 ? Ar.ReadByte() : -1;

            if (bCooked)
            {
                if (Ar.Ver >= UE4Version.VER_UE4_RENAME_CROUCHMOVESCHARACTERDOWN)
                {
                    bool Stripped = false;
                    if (Ar.Ver >= UE4Version.VER_UE4_RENAME_WIDGET_VISIBILITY)
                    {
                        FStripDataFlags stripDataFlags = Ar.Read<FStripDataFlags>();
                        Stripped = stripDataFlags.IsDataStrippedForServer();
                        if (Ar.Game >= EGame.GAME_UE4_21)
                        {
                            byte DistanceFieldDataStripFlag = 1;
                            Stripped |= stripDataFlags.IsClassDataStripped(DistanceFieldDataStripFlag);
                        }
                    }
                    if (!Stripped)
                    {
                        for (int i = 0; i < LODs.Length; i++)
                        {
                            bool hasDistanceDataField = Ar.ReadBoolean();
                            if (hasDistanceDataField)
                                new FDistanceFieldVolumeData(Ar);
                        }
                    }
                }
            }
            
            Bounds = Ar.Read<FBoxSphereBounds>();

            if (Ar.Game != EGame.GAME_UE4_15)
                LODsShareStaticLighting = Ar.ReadBoolean();

            bool bReducedBySimplygon;
            if (Ar.Game < EGame.GAME_UE4_15)
                bReducedBySimplygon = Ar.ReadBoolean();

            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.TextureStreamingMeshUVChannelData)
            {
                for (int i = 0; i < MAX_STATIC_UV_SETS_UE4; i++)
                {
                    var _ = Ar.Read<float>(); // StreamingTextureFactor for each UV set
                }
                Ar.Read<float>();  // MaxStreamingTextureFactor
            }

            if (bCooked)
            {
                int maxNumLods = Ar.Game >= EGame.GAME_UE4_9 ? MAX_STATIC_LODS_UE4 : 4;
                ScreenSize = new float[maxNumLods];
                for (int i = 0; i < maxNumLods; i++)
                {
                    if (Ar.Game >= EGame.GAME_UE4_20)
                        Ar.ReadBoolean(); // bFloatCooked
                    ScreenSize[i] = Ar.Read<float>();
                }
            }
        }
    }

    public class FStaticMeshRenderDataConverter : JsonConverter<FStaticMeshRenderData>
    {
        public override void WriteJson(JsonWriter writer, FStaticMeshRenderData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("LODs");
            writer.WriteStartObject();
            {
                for (int i = 0; i < value.LODs.Length; i++)
                {
                    writer.WritePropertyName(i.ToString());
                    serializer.Serialize(writer, value.LODs[i]);
                }
            }
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        public override FStaticMeshRenderData ReadJson(JsonReader reader, Type objectType, FStaticMeshRenderData existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
