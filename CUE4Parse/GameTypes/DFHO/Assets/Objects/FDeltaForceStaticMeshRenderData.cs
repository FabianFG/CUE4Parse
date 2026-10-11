using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DFHO.Assets.Objects;

public class FDeltaForceStaticMeshRenderData : FStaticMeshRenderData
{
    public FStaticMeshLODResources[] RayTracingLODs;
    public FStaticMeshLODResources[] FallbackLODs;

    public FDeltaForceStaticMeshRenderData(FAssetArchive Ar)
    {
        LODs = Ar.ReadArray(() => new FStaticMeshLODResources(Ar));
        RayTracingLODs = Ar.ReadArray(() => new FStaticMeshLODResources(Ar));
        FallbackLODs = Ar.ReadArray(() => new FStaticMeshLODResources(Ar));
        _ = Ar.Read<byte>(); // NumInlinedLODs

        FDeltaForceNaniteResources.Skip(Ar);

        var cardStripFlags = new FStripDataFlags(Ar);
        if (!cardStripFlags.IsAudioVisualDataStripped() && !cardStripFlags.IsClassDataStripped(2))
        {
            for (var i = 0; i < LODs.Length; i++)
            {
                if (!Ar.ReadBoolean()) continue;
                _ = new FBox(Ar); // Card bounds
                _ = Ar.ReadBoolean(); // bMostlyTwoSided
                Ar.SkipArray(() =>
                {
                    Ar.SkipArray<FVector>(5); // AxisX, AxisY, AxisZ, Origin, Extent
                    _ = Ar.Read<byte>(); // AxisAlignedDirectionIndex
                });
            }
        }

        var distanceFieldStripFlags = new FStripDataFlags(Ar);
        if (!distanceFieldStripFlags.IsAudioVisualDataStripped() && !distanceFieldStripFlags.IsClassDataStripped(1))
        {
            for (var i = 0; i < LODs.Length; i++)
            {
                if (Ar.ReadBoolean())
                {
                    _ = new FDistanceFieldVolumeData5(Ar);
                }
            }
        }

        Bounds = new FBoxSphereBounds(Ar);
        bLODsShareStaticLighting = Ar.ReadBoolean();
        _ = Ar.ReadBoolean(); // Custom
        ScreenSize = Ar.ReadArray(8, () => new FPerPlatformFloat(Ar).Value);
    }
}
