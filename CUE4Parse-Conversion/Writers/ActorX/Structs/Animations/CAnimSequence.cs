using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Writers.ActorX.Structs.Animations
{
    public class CAnimSequence
    {
        public UAnimSequence OriginalSequence;
        public readonly USkeleton Skeleton;
        public readonly FTransform[]? RetargetBasePose;

        public string Name;
        public string? SlotName;
        public readonly int NumFrames;
        public readonly float FramesPerSecond;
        public readonly bool IsAdditive;

        public float StartPos;
        public float AnimEndTime;
        public int LoopingCount;
        public List<CAnimTrack> Tracks;

        private bool _retargeted;
        internal bool _hasAdditiveTracks; // cleared once the additive tracks are converted to full poses

        public CAnimSequence(UAnimSequence animSequence, USkeleton skeleton)
        {
            OriginalSequence = animSequence;
            Skeleton = skeleton;
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
            _hasAdditiveTracks = IsAdditive;

            StartPos = 0.0f;
            AnimEndTime = OriginalSequence.SequenceLength;
            LoopingCount = 1;
            Tracks = new List<CAnimTrack>(OriginalSequence.GetNumTracks());
        }

        /// <summary>
        /// Retargets the animation onto a specific reference skeleton, modifying the tracks in place.
        /// This is a one-shot operation, and the tracks will only fit that skeleton afterward.
        /// </summary>
        /// <param name="target">
        /// The <see cref="USkinnedAsset.ReferenceSkeleton"/> of the mesh the animation will play on, not <see cref="USkeleton.ReferenceSkeleton"/>.
        /// </param>
        public void RetargetTracks(FReferenceSkeleton target)
        {
            if (_retargeted) return;

            var refSkeleton = Skeleton.ReferenceSkeleton;
            for (var skeletonBoneIndex = 0; skeletonBoneIndex < refSkeleton.FinalRefBonePose.Length; skeletonBoneIndex++)
            {
                if (skeletonBoneIndex >= Tracks.Count || skeletonBoneIndex >= Skeleton.BoneTree.Length || !Tracks[skeletonBoneIndex].HasKeys() ||
                    !target.FinalNameToIndexMap.TryGetValue(refSkeleton.FinalRefBoneInfo[skeletonBoneIndex].Name.Text, out var targetIndex))
                    continue;

                var track = Tracks[skeletonBoneIndex];
                var targetRefPose = target.FinalRefBonePose[targetIndex];
                var sourceRefPose = GetSourceRefPose(skeletonBoneIndex);
                var mode = Skeleton.BoneTree[skeletonBoneIndex];

                if (!_hasAdditiveTracks)
                {
                    // if a channel this mode retargets has no keys, add one so there is something to retarget
                    switch (mode)
                    {
                        case EBoneTranslationRetargetingMode.Skeleton:
                            if (track.KeyPos.Length == 0) track.KeyPos = [sourceRefPose.Translation];
                            break;
                        case EBoneTranslationRetargetingMode.AnimationRelative:
                            if (track.KeyQuat.Length == 0) track.KeyQuat = [sourceRefPose.Rotation];
                            if (track.KeyPos.Length == 0) track.KeyPos = [sourceRefPose.Translation];
                            if (track.KeyScale.Length == 0) track.KeyScale = [sourceRefPose.Scale3D];
                            break;
                    }
                }

                for (var i = 0; i < track.KeyQuat.Length; i++)
                    RetargetRotation(mode, sourceRefPose, targetRefPose, ref track.KeyQuat[i]);
                for (var i = 0; i < track.KeyPos.Length; i++)
                    RetargetTranslation(mode, sourceRefPose, targetRefPose, ref track.KeyPos[i]);
                for (var i = 0; i < track.KeyScale.Length; i++)
                    RetargetScale(mode, sourceRefPose, targetRefPose, ref track.KeyScale[i]);
            }

            _retargeted = true;
        }

        /// <summary>
        /// <see cref="RetargetTracks"/> but for a single bone, without modifying the tracks.
        /// you're supposed to call that after <see cref="CAnimTrack.GetBoneTransform"/> to get the retargeted transform for that bone.
        /// </summary>
        /// <param name="skeletonBoneIndex">the index of a bone in <see cref="Skeleton"/></param>
        /// <param name="targetRefPose">the transform of that same bone but now from <see cref="USkinnedAsset.ReferenceSkeleton"/></param>
        /// <param name="rotation">the rotation of that bone after <see cref="CAnimTrack.GetBoneTransform"/></param>
        /// <param name="translation">the translation of that bone after <see cref="CAnimTrack.GetBoneTransform"/></param>
        /// <param name="scale">the scale of that bone after <see cref="CAnimTrack.GetBoneTransform"/></param>
        public void RetargetBoneTransform(int skeletonBoneIndex, in FTransform targetRefPose, ref FQuat rotation, ref FVector translation, ref FVector scale)
        {
            if (_retargeted || skeletonBoneIndex < 0 || skeletonBoneIndex >= Skeleton.BoneTree.Length)
                return;

            var mode = Skeleton.BoneTree[skeletonBoneIndex];
            var sourceRefPose = GetSourceRefPose(skeletonBoneIndex);
            RetargetRotation(mode, sourceRefPose, targetRefPose, ref rotation);
            RetargetTranslation(mode, sourceRefPose, targetRefPose, ref translation);
            RetargetScale(mode, sourceRefPose, targetRefPose, ref scale);
        }

        private void RetargetRotation(EBoneTranslationRetargetingMode mode, in FTransform sourceRefPose, in FTransform targetRefPose, ref FQuat rotation)
        {
            // baked additive deltas cancel out: (A1 + Rel) - (A2 + Rel) = A1 - A2
            if (mode != EBoneTranslationRetargetingMode.AnimationRelative || _hasAdditiveTracks) return;

            rotation = rotation * sourceRefPose.Rotation.Inverse() * targetRefPose.Rotation;
            rotation.Normalize();
        }

        private void RetargetScale(EBoneTranslationRetargetingMode mode, in FTransform sourceRefPose, in FTransform targetRefPose, ref FVector scale)
        {
            if (mode != EBoneTranslationRetargetingMode.AnimationRelative || _hasAdditiveTracks) return;

            scale *= targetRefPose.Scale3D * FTransform.GetSafeScaleReciprocal(sourceRefPose.Scale3D);
        }

        private void RetargetTranslation(EBoneTranslationRetargetingMode mode, in FTransform sourceRefPose, in FTransform targetRefPose, ref FVector translation)
        {
            switch (mode)
            {
                case EBoneTranslationRetargetingMode.Skeleton:
                {
                    translation = _hasAdditiveTracks ? FVector.ZeroVector : targetRefPose.Translation;
                    break;
                }
                case EBoneTranslationRetargetingMode.AnimationScaled:
                {
                    var sourceTranslationLength = sourceRefPose.Translation.Size();
                    if (sourceTranslationLength > UnrealMath.KindaSmallNumber)
                    {
                        translation *= targetRefPose.Translation.Size() / sourceTranslationLength;
                    }
                    break;
                }
                case EBoneTranslationRetargetingMode.AnimationRelative:
                {
                    if (_hasAdditiveTracks) break;

                    translation += targetRefPose.Translation - sourceRefPose.Translation;
                    break;
                }
                case EBoneTranslationRetargetingMode.OrientAndScale:
                {
                    if (_hasAdditiveTracks) break;

                    var sourceSkelTrans = sourceRefPose.Translation;
                    var targetSkelTrans = targetRefPose.Translation;
                    if (sourceSkelTrans.Equals(targetSkelTrans)) break;

                    // translation isn't animated, nothing to retarget
                    if ((translation - sourceSkelTrans).IsNearlyZero(0.001f))
                    {
                        translation = targetSkelTrans;
                        break;
                    }

                    var sourceSkelTransLength = sourceSkelTrans.Size();
                    var targetSkelTransLength = targetSkelTrans.Size();
                    if (UnrealMath.IsNearlyZero(sourceSkelTransLength * targetSkelTransLength)) break;

                    var deltaRotation = FQuat.FindBetweenNormals(sourceSkelTrans / sourceSkelTransLength, targetSkelTrans / targetSkelTransLength);
                    translation = deltaRotation.RotateVector(translation) * (targetSkelTransLength / sourceSkelTransLength);
                    break;
                }
            }
        }

        private FTransform GetSourceRefPose(int skeletonBoneIndex)
        {
            return RetargetBasePose is not null && skeletonBoneIndex < RetargetBasePose.Length
                ? RetargetBasePose[skeletonBoneIndex]
                : Skeleton.ReferenceSkeleton.FinalRefBonePose[skeletonBoneIndex];
        }
    }
}
