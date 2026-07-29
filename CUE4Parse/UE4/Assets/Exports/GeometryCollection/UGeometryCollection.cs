using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Chaos;
using CUE4Parse.UE4.Objects.Chaos.GeometryCollection;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection;

public class UGeometryCollection : UObject
{
    public FGeometryCollectionProxyMeshData? RootProxyData { get; private set; }
    public FGeometryCollectionAutoInstanceMesh[]? AutoInstanceMeshes { get; private set; }
    public FGeometryCollection? GeometryCollection;
    public FGeometryCollectionRenderData? RenderData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        RootProxyData = GetOrDefault<FGeometryCollectionProxyMeshData?>(nameof(RootProxyData));
        AutoInstanceMeshes = GetOrDefault<FGeometryCollectionAutoInstanceMesh[]?>(nameof(AutoInstanceMeshes));

        var bIsCookedOrCooking = FDestructionObjectVersion.Get(Ar) >= FDestructionObjectVersion.Type.GeometryCollectionInDDC && Ar.ReadBoolean();
        if (FDestructionObjectVersion.Get(Ar) >= FDestructionObjectVersion.Type.GeometryCollectionInDDCAndAsset)
        {
            GeometryCollection = new FGeometryCollection(new FChaosArchive(Ar));
        }

        if (FUE5MainStreamObjectVersion.Get(Ar) == FUE5MainStreamObjectVersion.Type.GeometryCollectionNaniteData ||
            (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.GeometryCollectionNaniteCooked &&
             FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.GeometryCollectionNaniteTransient))
        {
            SerializeOldNaniteData(Ar);
        }

        if (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.GeometryCollectionNaniteTransient)
        {
            var bCooked = Ar.ReadBoolean();
            if (bCooked) RenderData = new FGeometryCollectionRenderData(Ar);
        }
    }

    private void SerializeOldNaniteData(FAssetArchive Ar)
    {
        var numNaniteResources = Ar.Read<int>();
        for (var i = 0; i < numNaniteResources; i++)
        {
            var stripFlags = new FStripDataFlags(Ar);
            if (!stripFlags.IsAudioVisualDataStripped())
            {
                Ar.Position += sizeof(int); // bLZCompressed
                Ar.SkipArray<byte>(); // RootClusterPage
                _ = new FByteBulkData(Ar); // StreamableClusterPages

                Ar.SkipFixedArray(5 * sizeof(uint)); // PageStreamingState
                Ar.SkipFixedArray(64 * 2 * Unsafe.SizeOf<FSphere>() + 3 * sizeof(uint)); // HierarchyNodes

                Ar.SkipArray<uint>(); // PageDependencies
                Ar.SkipArray<ushort>(); // ImposterAtlas
            }
        }
    }
}
