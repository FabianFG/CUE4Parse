using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FGPUVertFloat : FSkelMeshVertexBase
{
    private const int MAX_SKELETAL_UV_SETS_UE4 = 4;

    public sealed override FMeshUVFloat[] UVs { get; }

    public FGPUVertFloat()
    {
        UVs = [];
    }

    public FGPUVertFloat(FVector position, FPackedNormal[] normals, FSkinWeightInfo? infs, FMeshUVFloat[] uv) : base(position, normals, infs)
    {
        UVs = uv;
    }

    public FGPUVertFloat(FVector position, FSkinWeightInfo? infs, FStaticMeshUVItem item) : base(position, item.Normal, infs)
    {
        UVs = item.UV;
    }

    public FGPUVertFloat(FArchive Ar, bool bExtraBoneInfluences, int numSkelUVSets) : this()
    {
        SerializeForGPU(Ar, bExtraBoneInfluences);

        UVs = new FMeshUVFloat[MAX_SKELETAL_UV_SETS_UE4];
        for (var i = 0; i < numSkelUVSets; i++)
        {
            UVs[i] = Ar.Read<FMeshUVFloat>();
        }
    }
}
