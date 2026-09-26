using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.DeadIsland2.Assets.Objects;

public class FDeadIsland2StaticMeshRenderData : FStaticMeshRenderData
{
    public FDeadIsland2StaticMeshRenderData(FAssetArchive Ar)
    {
        // Count includes normal + platform LODs
        var totalLodCount = Ar.Read<int>();

        var lodCount = 0;
        var lods = new FStaticMeshLODResources[totalLodCount];
        for (var record = 0; record < totalLodCount;)
        {
            var lod = new FDeadIsland2StaticMeshLODResources(Ar);
            record++;
            if (lod.HasPlatformLODs)
            {
                for (var i = 0; i < 3 && record < totalLodCount; i++, record++)
                {
                    lod = new FDeadIsland2StaticMeshLODResources(Ar);
                }
            }

            lods[lodCount++] = lod;
        }

        LODs = lodCount == lods.Length ? lods : lods[..lodCount];

        _ = Ar.Read<byte>(); // NumInlinedLODs

        var stripFlags = new FStripDataFlags(Ar);
        if (!stripFlags.IsAudioVisualDataStripped() && !stripFlags.IsClassDataStripped(0x01) && Ar.ReadBoolean())
        {
            _ = new FDistanceFieldVolumeData(Ar);
        }

        Bounds = new FBoxSphereBounds(Ar);
        if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.TextureStreamingMeshUVChannelData)
        {
            Ar.Position += 4 * 8 + 4;
        }

        ScreenSize = new float[MAX_STATIC_LODS_UE4];
        for (var i = 0; i < ScreenSize.Length; i++)
        {
            ScreenSize[i] = new FPerPlatformFloat(Ar).Value;
        }
    }
}

public class FDeadIsland2StaticMeshLODResources : FStaticMeshLODResources
{
    internal bool HasPlatformLODs { get; private set; }

    public FDeadIsland2StaticMeshLODResources(FArchive Ar)
    {
        var stripFlags = new FStripDataFlags(Ar);

        Sections = Ar.ReadArray(() => new FStaticMeshSection(Ar));
        MaxDeviation = Ar.Read<float>();
        _ = Ar.Read<int>();

        if (Ar.ReadBoolean())
        {
            _ = Ar.ReadBoolean(); // Platform LODs flag
            HasPlatformLODs = true;
            return;
        }

        var bInlined = Ar.ReadBoolean();
        if (stripFlags.IsAudioVisualDataStripped())
            return;

        if (bInlined)
        {
            SerializeBuffers(Ar);
            Ar.Position += 4;
        }
        else if (Ar is FAssetArchive assetArchive)
        {
            var bulkData = new FByteBulkData(assetArchive);
            if (bulkData.Header.ElementCount > 0 && bulkData.Data != null)
            {
                using var bufferAr = new FByteArchive("DeadIsland2StaticMeshBuffer", bulkData.Data, Ar.Versions);
                SerializeBuffers(bufferAr);
            }

            Ar.Position += 8 + 4 * 4 + 2 * 4 + 2 * 4 + 5 * 2 * 4;
            if (FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.RemovingTessellation)
            {
                Ar.Position += 2 * 4;
            }
        }

        Ar.Position += 12; // FStaticMeshBuffersSize
    }
}
