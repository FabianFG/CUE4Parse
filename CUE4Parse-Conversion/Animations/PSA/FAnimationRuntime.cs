using System;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Animations.PSA
{
    public static class FAnimationRuntime
    {
        public static FCompactPose[] LoadRestAsPoses(CAnimSet anim)
        {
            var poses = new FCompactPose[1];
            for (int frameIndex = 0; frameIndex < poses.Length; frameIndex++)
            {
                poses[frameIndex] = new FCompactPose(anim.BonePositions.Length);
                for (var boneIndex = 0; boneIndex < poses[frameIndex].Bones.Length; boneIndex++)
                {
                    var boneInfo = anim.TrackBonesInfo[boneIndex];
                    poses[frameIndex].Bones[boneIndex] = new FPoseBone
                    {
                        Name = boneInfo.Name.ToString(),
                        ParentIndex = boneInfo.ParentIndex,
                        Transform = (FTransform)anim.BonePositions[boneIndex].Clone(),
                        IsValidKey = true
                    };
                }
            }
            return poses;
        }

        public static FCompactPose[] LoadAsPoses(CAnimSet anim, int refFrame)
        {
            var seq = anim.Sequences[0];
            var poses = new FCompactPose[1];
            for (int frameIndex = 0; frameIndex < poses.Length; frameIndex++)
            {
                poses[frameIndex] = new FCompactPose(anim.BonePositions.Length);
                for (var boneIndex = 0; boneIndex < poses[frameIndex].Bones.Length; boneIndex++)
                {
                    var boneInfo = anim.TrackBonesInfo[boneIndex];
                    var originalTransform = anim.BonePositions[boneIndex];
                    var track = seq.Tracks[boneIndex];

                    var boneOrientation = FQuat.Identity;
                    var bonePosition = FVector.ZeroVector;
                    var boneScale = FVector.OneVector;

                    track.GetBonePosition(refFrame, seq.NumFrames, false, ref bonePosition, ref boneOrientation);
                    if (refFrame < seq.Tracks[boneIndex].KeyScale.Length)
                        boneScale = seq.Tracks[boneIndex].KeyScale[refFrame];

                    switch (anim.BoneModes[boneIndex])
                    {
                        case EBoneTranslationRetargetingMode.Skeleton:
                        {
                            var targetTransform = seq.RetargetBasePose?[boneIndex] ?? originalTransform;
                            bonePosition = targetTransform.Translation;
                            break;
                        }
                        case EBoneTranslationRetargetingMode.AnimationScaled:
                        {
                            var sourceTranslationLength = originalTransform.Translation.Size();
                            if (sourceTranslationLength > UnrealMath.KindaSmallNumber)
                            {
                                var targetTranslationLength = seq.RetargetBasePose?[boneIndex].Translation.Size() ?? sourceTranslationLength;
                                bonePosition.Scale(targetTranslationLength / sourceTranslationLength);
                            }
                            break;
                        }
                        case EBoneTranslationRetargetingMode.AnimationRelative:
                        {
                            // can't tell if it's working or not
                            var sourceSkelTrans = originalTransform.Translation;
                            var refPoseTransform  = seq.RetargetBasePose?[boneIndex] ?? originalTransform;

                            boneOrientation = boneOrientation * FQuat.Conjugate(originalTransform.Rotation) * refPoseTransform.Rotation;
                            bonePosition += refPoseTransform.Translation - sourceSkelTrans;
                            boneScale *= refPoseTransform.Scale3D * originalTransform.Scale3D;
                            boneOrientation.Normalize();
                            break;
                        }
                        case EBoneTranslationRetargetingMode.OrientAndScale:
                        {
                            var sourceSkelTrans = originalTransform.Translation;
                            var targetSkelTrans = seq.RetargetBasePose?[boneIndex].Translation ?? sourceSkelTrans;

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

        public static FCompactPose[] LoadAsPoses(CAnimSet anim)
        {
            var seq = anim.Sequences[0];
            var poses = new FCompactPose[seq.NumFrames];
            for (int frameIndex = 0; frameIndex < poses.Length; frameIndex++)
            {
                poses[frameIndex] = new FCompactPose(anim.BonePositions.Length);
                for (var boneIndex = 0; boneIndex < poses[frameIndex].Bones.Length; boneIndex++)
                {
                    var boneInfo = anim.TrackBonesInfo[boneIndex];
                    var track = seq.Tracks[boneIndex];

                    var boneOrientation = FQuat.Identity;
                    var bonePosition = FVector.ZeroVector;
                    var boneScale = FVector.ZeroVector;

                    track.GetBonePosition(frameIndex, seq.NumFrames, false, ref bonePosition, ref boneOrientation);
                    if (frameIndex < seq.Tracks[boneIndex].KeyScale.Length)
                        boneScale = seq.Tracks[boneIndex].KeyScale[frameIndex];

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
