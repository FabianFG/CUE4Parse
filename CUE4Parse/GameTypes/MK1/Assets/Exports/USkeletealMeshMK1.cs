using System;
using CUE4Parse.GameTypes.MK1.Assets.Objects;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public partial class USkeletalMesh : UObject
{
    public void PopulateMorphTargetVerticesDataMK1()
    {
        if (LODModels![0] is null || LODModels[0].Sections.Length == 0)
            LODModels = LODModels[1..];

        var maxLodLevel = -1;
        for (int i = 0; i < LODModels.Length; i++)
        {
            if (LODModels[i].AdditionalBuffer is not null)
            {
                maxLodLevel = i + 1;
            }
        }

        if (maxLodLevel == -1)
            return;

        for (int index = 0; index < MorphTargets.Length; index++)
        {
            if (!MorphTargets[index].TryLoad(out UMorphTarget morphTarget)) continue;

            var morphTargetLODModels = new FMorphTargetLODModel[maxLodLevel];
            for (var i = 0; i < maxLodLevel; i++)
            {
                morphTargetLODModels[i] = new FMorphTargetLODModel();
                if (LODModels[i].AdditionalBuffer is not FMorphTargetVertexInfoBufferMK1 buffer) continue;

                var size = buffer.Sizes[index];
                morphTargetLODModels[i].NumBaseMeshVerts = size;
                morphTargetLODModels[i].Vertices = new FMorphTargetDelta[size];
                Array.Copy(buffer.Vertices, buffer.Offsets[index], morphTargetLODModels[i].Vertices, 0, size);
            }

            morphTarget.MorphLODModels = morphTargetLODModels;
        }
    }
}
