using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteConstants;

namespace CUE4Parse.GameTypes.DFHO.Assets.Objects;

public static class FDeltaForceNaniteResources
{
    public static void Skip(FAssetArchive Ar)
    {
        var stripFlags = new FStripDataFlags(Ar);
        if (stripFlags.IsAudioVisualDataStripped())
            return;

        var resourceFlags = Ar.Read<NANITE_RESOURCE_FLAG>();
        if (!resourceFlags.HasFlag(NANITE_RESOURCE_FLAG.STREAMING_DATA_IN_DDC))
            _ = new FByteBulkData(Ar); // StreamablePages

        Ar.SkipArray<byte>(); // RootData
        Ar.SkipFixedArray(20); // PageStreamingStates
        Ar.SkipFixedArray(224); // HierarchyNodes
        Ar.SkipArray<uint>(); // HierarchyRootOffsets
        Ar.SkipArray<ushort>(); // PageDependencies
        Ar.SkipFixedArray(48); // AssemblyTransforms
        Ar.SkipArray<uint>(); // Custom metadata
        Ar.SkipArray<FVector4>(); // Custom metadata
        Ar.SkipArray<uint>(); // Custom metadata
        _ = new FBoxSphereBounds(Ar); // MeshBounds
        Ar.SkipArray<ushort>(); // ImposterAtlas
        _ = Ar.Read<int>(); // NumRootPages
        _ = Ar.Read<int>(); // PositionPrecision
        _ = Ar.Read<int>(); // NormalPrecision
        _ = Ar.Read<uint>(); // NumInputTriangles
        _ = Ar.Read<uint>(); // NumInputVertices
        _ = Ar.Read<ushort>(); // NumInputMeshes
        _ = Ar.Read<ushort>(); // NumInputTexCoords
        _ = Ar.Read<uint>(); // NumClusters
        _ = Ar.Read<ulong>(); // Custom metadata
        Ar.SkipArray(Ar.SkipArray<FVector>); // Custom metadata
        Ar.SkipArray(Ar.SkipArray<uint>); // Custom metadata
        Ar.SkipArray(Ar.SkipArray<FVector2D>); // Custom metadata
        Ar.SkipArray(Ar.SkipArray<FVector4>); // Custom metadata
        Ar.SkipArray(Ar.SkipArray<uint>); // Custom metadata
        Ar.SkipArray<uint>(); // Custom metadata
        _ = Ar.Read<uint>(); // Custom metadata
        _ = Ar.Read<uint>(); // Custom metadata
        Ar.SkipArray<uint>(); // Custom metadata
    }
}
