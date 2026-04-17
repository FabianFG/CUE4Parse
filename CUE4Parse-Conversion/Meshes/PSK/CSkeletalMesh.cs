using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Meshes.PSK;

/// <summary>
/// TODO: this needs a refactor
/// </summary>
public class CSkeletalMesh : CMesh<CSkelMeshLod>
{
    public readonly List<CSkelMeshBone> RefSkeleton = [];

    public CSkeletalMesh(FBoxSphereBounds bounds) : base(bounds)
    {

    }

    public override void FinalizeMesh()
    {
        base.FinalizeMesh();

        // SortBones();
        // FixBoneWeights();
    }

    public override void Dispose()
    {
        base.Dispose();

        RefSkeleton.Clear();
    }
}
