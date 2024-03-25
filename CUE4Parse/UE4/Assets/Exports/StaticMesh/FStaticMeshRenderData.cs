using CUE4Parse.UE4.Assets.Exports.Nanite;
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
        public readonly FNaniteResources? NaniteResources;
        public readonly FBoxSphereBounds Bounds;
        public readonly bool bLODsShareStaticLighting;
        public readonly float[]? ScreenSize;

        public FStaticMeshRenderData(FAssetArchive Ar, bool bCooked)
        {
            if (!bCooked) return;

            if (Ar.Versions["StaticMesh.KeepMobileMinLODSettingOnDesktop"])
            {
                var minMobileLODIdx = Ar.Read<int>();
            }

            if (Ar.Game == EGame.GAME_HYENAS) Ar.Position += 1;

            LODs = Ar.ReadArray(() => new FStaticMeshLODResources(Ar));
            if (Ar.Game >= EGame.GAME_UE4_23)
            {
                var numInlinedLODs = Ar.Read<byte>();
            }

            if (Ar.Game >= EGame.GAME_UE5_0)
            {
                NaniteResources = new FNaniteResources(Ar);
                SerializeInlineDataRepresentations(Ar);
            }

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.RENAME_CROUCHMOVESCHARACTERDOWN)
            {
                var stripped = false;
                if (Ar.Ver >= EUnrealEngineObjectUE4Version.RENAME_WIDGET_VISIBILITY)
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
                            if (Ar.Game >= EGame.GAME_UE5_0)
                            {
                                _ = new FDistanceFieldVolumeData5(Ar);
                            }
                            else
                            {
                                _ = new FDistanceFieldVolumeData(Ar);
                            }
                        }
                    }
                }
            }

            Bounds = new FBoxSphereBounds(Ar);

            if (Ar.Versions["StaticMesh.HasLODsShareStaticLighting"])
                bLODsShareStaticLighting = Ar.ReadBoolean();

            if (Ar.Game < EGame.GAME_UE4_14)
            {
                var bReducedBySimplygon = Ar.ReadBoolean();
            }

            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.TextureStreamingMeshUVChannelData)
            {
                Ar.Position += 4 * MAX_STATIC_UV_SETS_UE4; // StreamingTextureFactor for each UV set
                Ar.Position += 4; // MaxStreamingTextureFactor
            }

            ScreenSize = new float[Ar.Game >= EGame.GAME_UE4_9 ? MAX_STATIC_LODS_UE4 : 4];
            for (var i = 0; i < ScreenSize.Length; i++)
            {
                if (Ar.Game >= EGame.GAME_UE4_20) // FPerPlatformProperty
                {
                    var bFloatCooked = Ar.ReadBoolean();
                }

                ScreenSize[i] = Ar.Read<float>();

                if (Ar.Game == EGame.GAME_HogwartsLegacy) Ar.Position +=8;
            }

            if (Ar.Game == EGame.GAME_Borderlands3)
            {
                var count = Ar.Read<int>();
                for (var i = 0; i < count; i++)
                {
                    var count2 = Ar.Read<byte>();
                    Ar.Position += count2 * 12; // bool, bool, float
                }
            }

            if (Ar.Game >= EGame.GAME_UE5_4) _ = Ar.Read<FStripDataFlags>();
        }

        private void SerializeInlineDataRepresentations(FAssetArchive Ar)
        {
            // Defined class flags for possible stripping
            const byte CardRepresentationDataStripFlag = 2;

            var stripFlags = new FStripDataFlags(Ar);
            if (!stripFlags.IsDataStrippedForServer() && !stripFlags.IsClassDataStripped(CardRepresentationDataStripFlag))
            {
                foreach (var lod in LODs)
                {
                    var bValid = Ar.ReadBoolean();
                    if (bValid)
                    {
                        lod.CardRepresentationData = new FCardRepresentationData(Ar);
                    }
                }
            }
        }
    }
}
