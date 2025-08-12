using System;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

[JsonConverter(typeof(FStaticMeshRenderDataConverter))]
public class FStaticMeshRenderData
{
    protected const int MAX_STATIC_UV_SETS_UE4 = 8;
    protected const int MAX_STATIC_LODS_UE4 = 8;

    public FStaticMeshLODResources[]? LODs;
    public FNaniteResources? NaniteResources;
    public FBoxSphereBounds? Bounds;
    public bool bLODsShareStaticLighting;
    public float[] ScreenSize = [];

    public FStaticMeshRenderData() { }

    public FStaticMeshRenderData(FAssetArchive Ar)
    {
        if (Ar.Versions["StaticMesh.KeepMobileMinLODSettingOnDesktop"])
            _ = Ar.Read<int>(); // minMobileLODIdx

        if (Ar.Game == EGame.GAME_TonyHawkProSkater34 && !Ar.ReadBoolean()) return;

        Ar.Position += Ar.Game switch
        {
            EGame.GAME_HYENAS => 1,
            EGame.GAME_DuneAwakening => 4,
            EGame.GAME_DaysGone => Ar.Read<int>() * 4,
            _ => 0
        };

        if (Ar.Game == EGame.GAME_Undawn)
        {
            var size = Ar.Read<int>();
            LODs = new FStaticMeshLODResources[size];
            for (var i = 0; i < size; i++)
            {
                var savedPos = Ar.Position;
                var bulkData = new FByteBulkData(Ar);
                if (bulkData.Header.ElementCount > 0 && bulkData.Data != null)
                {
                    var tempAr = new FByteArchive("StaticMeshLODResources", bulkData.Data, Ar.Versions);
                    LODs[i] = new FStaticMeshLODResources(tempAr);
                }
                else
                {
                    Ar.Position = savedPos;
                    LODs[i] = new FStaticMeshLODResources(Ar);
                }
            }
        }
        else
        {
            LODs = Ar.ReadArray(() => new FStaticMeshLODResources(Ar));
        }

        // In Fortnite S8, engine is 4.22, but has static mesh from 4.23.
        // Comment this check out to fix.
        if (Ar.Game >= EGame.GAME_UE4_23)
        {
            var numInlinedLODs = Ar.Read<byte>();
        }

        if (Ar.Game >= EGame.GAME_UE5_0)
        {
            NaniteResources = new FNaniteResources(Ar);

            if (Ar.Game >= EGame.GAME_UE5_5)
            {
                var bHasRayTracingProxy = Ar.ReadBoolean();
                if (bHasRayTracingProxy)
                {
                    var rayTracingProxy = new FStaticMeshRayTracingProxy(Ar);
                }
            }

            SerializeInlineDataRepresentations(Ar);
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.RENAME_CROUCHMOVESCHARACTERDOWN)
        {
            var stripped = false;
            if (Ar.Ver >= EUnrealEngineObjectUE4Version.RENAME_WIDGET_VISIBILITY)
            {
                var stripDataFlags = Ar.Read<FStripDataFlags>();
                stripped = stripDataFlags.IsAudioVisualDataStripped();
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
                        if (Ar.Game is >= EGame.GAME_UE5_0 or EGame.GAME_TerminullBrigade)
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
        {
            if (Ar.Game >= EGame.GAME_UE5_6)
            {
                var bRenderDataFlags = Ar.Read<byte>();
                bLODsShareStaticLighting = (bRenderDataFlags & 1) != 0;
            }
            else
            {
                bLODsShareStaticLighting = Ar.ReadBoolean();
            }
        }

        if (Ar.Game < EGame.GAME_UE4_14)
            _ = Ar.ReadBoolean();

        if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.TextureStreamingMeshUVChannelData)
        {
            Ar.Position += 4 * MAX_STATIC_UV_SETS_UE4; // StreamingTextureFactor for each UV set
            Ar.Position += 4; // MaxStreamingTextureFactor
        }

        if (Ar.Game is EGame.GAME_DeltaForceHawkOps or EGame.GAME_DeadzoneRogue) Ar.Position += 4;
        if (Ar.Game is EGame.GAME_InfinityNikki) Ar.Position += 8;

        var screenSizeLength = Ar.Game switch
        {
            EGame.GAME_FragPunk => 16,
            EGame.GAME_Stalker2 => 14,
            >= EGame.GAME_UE4_9 => MAX_STATIC_LODS_UE4,
            _ => 4
        };
        ScreenSize = new float[screenSizeLength];
        for (var i = 0; i < ScreenSize.Length; ++i)
        {
            if (Ar.Game >= EGame.GAME_UE4_20) // FPerPlatformProperty
            {
                var bFloatCooked = Ar.ReadBoolean();
            }

            ScreenSize[i] = Ar.Read<float>();

            if (Ar.Game == EGame.GAME_HogwartsLegacy) Ar.Position += 8;
            if (Ar.Game == EGame.GAME_VisionsofMana) Ar.Position += 4;
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

        if (Ar.Game == EGame.GAME_DaysGone)
        {
            const float packed64scale = 2.0f / ushort.MaxValue;
            const float packed32scale = 2.0f / 1024;
            var offset = Bounds.Origin - Bounds.BoxExtent;
            var scale = Bounds.BoxExtent;
            foreach (var lod in LODs)
            {
                var perlodscale = lod.PositionVertexBuffer?.Stride switch
                {
                    4 => scale * packed32scale,
                    8 => scale * packed64scale,
                    12 => scale,
                    _ => throw new ArgumentOutOfRangeException($"Unknown stride {lod.PositionVertexBuffer?.Stride} for FPositionVertexBuffer")
                };

                for (var i = 0; i < lod.PositionVertexBuffer.NumVertices; i++)
                {
                    lod.PositionVertexBuffer.Verts[i] = lod.PositionVertexBuffer.Verts[i] * perlodscale + offset;
                }
            }
        }

        if (Ar.Game >= EGame.GAME_UE5_4) _ = Ar.Read<FStripDataFlags>();
    }

    private void SerializeInlineDataRepresentations(FAssetArchive Ar)
    {
        // Defined class flags for possible stripping
        const byte CardRepresentationDataStripFlag = 2;

        var stripFlags = new FStripDataFlags(Ar);
        if (!stripFlags.IsAudioVisualDataStripped() && !stripFlags.IsClassDataStripped(CardRepresentationDataStripFlag))
        {
            foreach (var lod in LODs ?? [])
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
