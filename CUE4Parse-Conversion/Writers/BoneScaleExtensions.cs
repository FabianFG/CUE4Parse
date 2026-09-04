using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Writers;

public static class BoneScaleExtensions
{
    public static FVector[] GetParentScales(this MeshBoneDto[] bones)
    {
        var scales = new FVector[bones.Length];
        for (var boneIndex = 0; boneIndex < bones.Length; boneIndex++)
        {
            var scale = FVector.OneVector;
            for (var parent = bones[boneIndex].ParentIndex; parent >= 0; parent = bones[parent].ParentIndex)
            {
                scale.Scale(bones[parent].Transform.Scale3D);
            }
            scales[boneIndex] = scale;
        }
        return scales;
    }

    public static FVector[] GetParentScales(this FReferenceSkeleton skeleton, FTransform[] pose)
    {
        var bones = skeleton.FinalRefBoneInfo;
        var scales = new FVector[bones.Length];
        for (var boneIndex = 0; boneIndex < bones.Length; boneIndex++)
        {
            var scale = FVector.OneVector;
            for (var parent = bones[boneIndex].ParentIndex; parent >= 0; parent = bones[parent].ParentIndex)
            {
                scale.Scale(pose[parent].Scale3D);
            }
            scales[boneIndex] = scale;
        }
        return scales;
    }

    public static FVector[] GetParentScales(this FReferenceSkeleton skeleton, CAnimSequence sequence)
    {
        var refPose = skeleton.FinalRefBonePose;
        var pose = sequence.RetargetBasePose is { } basePose && basePose.Length == refPose.Length ? basePose : refPose;
        return skeleton.GetParentScales(pose);
    }

    public static FVector RelativeTo(this FVector scale, FVector restScale)
    {
        return new FVector(
            UnrealMath.IsNearlyZero(restScale.X) ? scale.X : scale.X / restScale.X,
            UnrealMath.IsNearlyZero(restScale.Y) ? scale.Y : scale.Y / restScale.Y,
            UnrealMath.IsNearlyZero(restScale.Z) ? scale.Z : scale.Z / restScale.Z);
    }
}
