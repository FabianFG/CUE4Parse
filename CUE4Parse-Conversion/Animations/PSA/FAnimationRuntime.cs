using System;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Animations.PSA
{
    public static class FAnimationRuntime
    {
        public static FCompactPose[] LoadRestAsPoses(USkeleton skeleton)
        {
            var poses = new FCompactPose[1];
            for (int frameIndex = 0; frameIndex < poses.Length; frameIndex++)
            {
                poses[frameIndex] = new FCompactPose(skeleton.BoneCount);
                for (var boneIndex = 0; boneIndex < poses[frameIndex].Bones.Length; boneIndex++)
                {
                    var boneInfo = skeleton.ReferenceSkeleton.FinalRefBoneInfo[boneIndex];
                    poses[frameIndex].Bones[boneIndex] = new FPoseBone
                    {
                        Name = boneInfo.Name.ToString(),
                        ParentIndex = boneInfo.ParentIndex,
                        Transform = (FTransform)skeleton.ReferenceSkeleton.FinalRefBonePose[boneIndex].Clone(),
                        IsValidKey = true
                    };
                }
            }
            return poses;
        }

        public static FCompactPose[] LoadAsPoses(CAnimSequence sequence, USkeleton skeleton, int refFrame)
        {
            var poses = new FCompactPose[1];
            for (int frameIndex = 0; frameIndex < poses.Length; frameIndex++)
            {
                poses[frameIndex] = new FCompactPose(skeleton.BoneCount);
                for (var boneIndex = 0; boneIndex < poses[frameIndex].Bones.Length; boneIndex++)
                {
                    var boneInfo = skeleton.ReferenceSkeleton.FinalRefBoneInfo[boneIndex];
                    var originalTransform = skeleton.ReferenceSkeleton.FinalRefBonePose[boneIndex];
                    var track = sequence.Tracks[boneIndex];

                    var boneOrientation = FQuat.Identity;
                    var bonePosition = FVector.ZeroVector;
                    var boneScale = FVector.OneVector;

                    track.GetBoneTransform(refFrame, sequence.NumFrames, ref boneOrientation, ref bonePosition, ref boneScale);

                    switch (skeleton.BoneTree[boneIndex])
                    {
                        case EBoneTranslationRetargetingMode.Skeleton:
                        {
                            var targetTransform = sequence.RetargetBasePose?[boneIndex] ?? originalTransform;
                            bonePosition = targetTransform.Translation;
                            break;
                        }
                        case EBoneTranslationRetargetingMode.AnimationScaled:
                        {
                            var sourceTranslationLength = originalTransform.Translation.Size();
                            if (sourceTranslationLength > UnrealMath.KindaSmallNumber)
                            {
                                var targetTranslationLength = sequence.RetargetBasePose?[boneIndex].Translation.Size() ?? sourceTranslationLength;
                                bonePosition.Scale(targetTranslationLength / sourceTranslationLength);
                            }
                            break;
                        }
                        case EBoneTranslationRetargetingMode.AnimationRelative:
                        {
                            // can't tell if it's working or not
                            var sourceSkelTrans = originalTransform.Translation;
                            var refPoseTransform  = sequence.RetargetBasePose?[boneIndex] ?? originalTransform;

                            boneOrientation = boneOrientation * FQuat.Conjugate(originalTransform.Rotation) * refPoseTransform.Rotation;
                            bonePosition += refPoseTransform.Translation - sourceSkelTrans;
                            boneScale *= refPoseTransform.Scale3D * originalTransform.Scale3D;
                            boneOrientation.Normalize();
                            break;
                        }
                        case EBoneTranslationRetargetingMode.OrientAndScale:
                        {
                            var sourceSkelTrans = originalTransform.Translation;
                            var targetSkelTrans = sequence.RetargetBasePose?[boneIndex].Translation ?? sourceSkelTrans;

                            if (!sourceSkelTrans.Equals(targetSkelTrans))
                            {
                                var sourceSkelTransLength = sourceSkelTrans.Size();
                                var targetSkelTransLength = targetSkelTrans.Size();
                                if (!UnrealMath.IsNearlyZero(sourceSkelTransLength * targetSkelTransLength))
                                {
                                    var sourceSkelTransDir = sourceSkelTrans / sourceSkelTransLength;
                                    var targetSkelTransDir = targetSkelTrans / targetSkelTransLength;

                                    var deltaRotation = FQuat.FindBetweenNormals(sourceSkelTransDir, targetSkelTransDir);
                                    var scale = targetSkelTransLength / sourceSkelTransLength;
                                    bonePosition = deltaRotation.RotateVector(bonePosition) * scale;
                                }
                            }
                            break;
                        }
                    }

                    poses[frameIndex].Bones[boneIndex] = new FPoseBone
                    {
                        Name = boneInfo.Name.ToString(),
                        ParentIndex = boneInfo.ParentIndex,
                        Transform = new FTransform(boneOrientation, bonePosition, boneScale),
                        IsValidKey = true
                    };
                }
            }
            return poses;
        }

        public static FCompactPose[] LoadAsPoses(CAnimSequence sequence, USkeleton skeleton)
        {
            var poses = new FCompactPose[sequence.NumFrames];
            for (int frameIndex = 0; frameIndex < poses.Length; frameIndex++)
            {
                poses[frameIndex] = new FCompactPose(skeleton.BoneCount);
                for (var boneIndex = 0; boneIndex < poses[frameIndex].Bones.Length; boneIndex++)
                {
                    var boneInfo = skeleton.ReferenceSkeleton.FinalRefBoneInfo[boneIndex];
                    var track = sequence.Tracks[boneIndex];

                    var boneOrientation = FQuat.Identity;
                    var bonePosition = FVector.ZeroVector;
                    var boneScale = FVector.ZeroVector;

                    track.GetBoneTransform(frameIndex, sequence.NumFrames, ref boneOrientation, ref bonePosition, ref boneScale);

                    poses[frameIndex].Bones[boneIndex] = new FPoseBone
                    {
                        Name = boneInfo.Name.ToString(),
                        ParentIndex = boneInfo.ParentIndex,
                        Transform = new FTransform(boneOrientation, bonePosition, boneScale),
                        IsValidKey = frameIndex <= Math.Min(track.KeyPos.Length, track.KeyQuat.Length)
                    };
                }
            }
            return poses;
        }

        public static void AccumulateLocalSpaceAdditivePoseInternal(FCompactPose basePose, FCompactPose additivePose, float weight)
        {
            if (weight < 0.999989986419678)
                throw new NotImplementedException();

            for (int index = 0; index < basePose.Bones.Length; index++)
            {
                basePose.Bones[index].AccumulateWithAdditiveScale(additivePose.Bones[index].Transform, weight);
            }
        }

        public static void AccumulateMeshSpaceRotationAdditiveToLocalPoseInternal(FCompactPose basePose, FCompactPose additivePose, float weight)
        {
            ConvertPoseToMeshRotation(basePose);
            AccumulateLocalSpaceAdditivePoseInternal(basePose, additivePose, weight);
            ConvertMeshRotationPoseToLocalSpace(basePose);
        }

        public static void ConvertPoseToMeshRotation(FCompactPose localPose)
        {
            for (var boneIndex = 1; boneIndex < localPose.Bones.Length; ++boneIndex)
            {
                var parentIndex = localPose.Bones[boneIndex].ParentIndex;
                var meshSpaceRotation = localPose.Bones[parentIndex].Transform.Rotation * localPose.Bones[boneIndex].Transform.Rotation;
                localPose.Bones[boneIndex].Transform.Rotation = meshSpaceRotation;
            }
        }

        public static void ConvertMeshRotationPoseToLocalSpace(FCompactPose pose)
        {
            for (var boneIndex = pose.Bones.Length - 1; boneIndex > 0; --boneIndex)
            {
                var parentIndex = pose.Bones[boneIndex].ParentIndex;
                var localSpaceRotation = pose.Bones[parentIndex].Transform.Rotation.Inverse() * pose.Bones[boneIndex].Transform.Rotation;
                pose.Bones[boneIndex].Transform.Rotation = localSpaceRotation;
            }
        }
    }
}
