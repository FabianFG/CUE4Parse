using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Animations.PSA
{
    public class CAnimSequence
    {
        public UAnimSequence OriginalSequence;
        public readonly FTransform[]? RetargetBasePose;

        public string Name;
        public readonly int NumFrames;
        public readonly float FramesPerSecond;
        public readonly bool IsAdditive;

        public float StartPos;
        public float AnimEndTime;
        public int LoopingCount;
        public List<CAnimTrack> Tracks;

        public CAnimSequence(UAnimSequence animSequence, USkeleton skeleton)
        {
            OriginalSequence = animSequence;
            RetargetBasePose = OriginalSequence.RetargetSource.IsNone switch
            {
                true when OriginalSequence.RetargetSourceAssetReferencePose is { Length: > 0 }
                    => OriginalSequence.RetargetSourceAssetReferencePose,
                false when skeleton.AnimRetargetSources.TryGetValue(OriginalSequence.RetargetSource, out var refPose)
                    => refPose.ReferencePose,
                _ => null
            };

            Name = OriginalSequence.Name;
            NumFrames = OriginalSequence.NumFrames;
            FramesPerSecond = OriginalSequence.NumFrames / OriginalSequence.SequenceLength * MathF.Max(1, OriginalSequence.RateScale);
            IsAdditive = OriginalSequence.AdditiveAnimType != EAdditiveAnimationType.AAT_None;

            StartPos = 0.0f;
            AnimEndTime = OriginalSequence.SequenceLength;
            LoopingCount = 1;
            Tracks = new List<CAnimTrack>(OriginalSequence.GetNumTracks());
        }

        public void RetargetTracks(USkeleton skeleton)
        {
            for (int skeletonBoneIndex = 0; skeletonBoneIndex < Tracks.Count; skeletonBoneIndex++)
            {
                switch (skeleton.BoneTree[skeletonBoneIndex])
                {
                    case EBoneTranslationRetargetingMode.Skeleton:
                    {
                        var targetTransform = RetargetBasePose?[skeletonBoneIndex] ?? skeleton.ReferenceSkeleton.FinalRefBonePose[skeletonBoneIndex];
                        for (int i = 0; i < Tracks[skeletonBoneIndex].KeyPos.Length; i++)
                        {
                            Tracks[skeletonBoneIndex].KeyPos[i] = targetTransform.Translation;
                        }
                        break;
                    }
                    case EBoneTranslationRetargetingMode.AnimationScaled:
                    {
                        for (int i = 0; i < Tracks[skeletonBoneIndex].KeyPos.Length; i++)
                        {
                            var sourceTranslationLength = skeleton.ReferenceSkeleton.FinalRefBonePose[skeletonBoneIndex].Translation.Size();
                            if (sourceTranslationLength > UnrealMath.KindaSmallNumber)
                            {
                                var targetTranslationLength = RetargetBasePose?[skeletonBoneIndex].Translation.Size() ?? sourceTranslationLength;
                                Tracks[skeletonBoneIndex].KeyPos[i].Scale(targetTranslationLength / sourceTranslationLength);
                            }
                        }
                        break;
                    }
                    case EBoneTranslationRetargetingMode.AnimationRelative:
                    {
                        var refPoseTransform  = RetargetBasePose?[skeletonBoneIndex] ?? skeleton.ReferenceSkeleton.FinalRefBonePose[skeletonBoneIndex];
                        for (int i = 0; i < Tracks[skeletonBoneIndex].KeyQuat.Length; i++)
                        {
                            Tracks[skeletonBoneIndex].KeyQuat[i] = Tracks[skeletonBoneIndex].KeyQuat[i] * FQuat.Conjugate(Tracks[skeletonBoneIndex].KeyQuat[i]) * refPoseTransform.Rotation;
                            Tracks[skeletonBoneIndex].KeyQuat[i].Normalize();
                        }
                        for (int i = 0; i < Tracks[skeletonBoneIndex].KeyPos.Length; i++)
                        {
                            Tracks[skeletonBoneIndex].KeyPos[i] += refPoseTransform.Translation - Tracks[skeletonBoneIndex].KeyPos[i];
                        }
                        for (int i = 0; i < Tracks[skeletonBoneIndex].KeyScale.Length; i++)
                        {
                            Tracks[skeletonBoneIndex].KeyScale[i] *= refPoseTransform.Scale3D * Tracks[skeletonBoneIndex].KeyScale[i];
                        }
                        break;
                    }
                    case EBoneTranslationRetargetingMode.OrientAndScale:
                    {
                        var sourceSkelTrans = skeleton.ReferenceSkeleton.FinalRefBonePose[skeletonBoneIndex].Translation;
                        var targetSkelTrans = RetargetBasePose?[skeletonBoneIndex].Translation ?? sourceSkelTrans;

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
                                for (int i = 0; i < Tracks[skeletonBoneIndex].KeyPos.Length; i++)
                                {
                                    Tracks[skeletonBoneIndex].KeyPos[i] = deltaRotation.RotateVector(Tracks[skeletonBoneIndex].KeyPos[i]) * scale;
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}
