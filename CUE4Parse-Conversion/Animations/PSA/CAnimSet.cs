using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Animations.PSA
{
    public class CAnimSet
    {
        public UObject OriginalAnim;
        public FMeshBoneInfo[] TrackBonesInfo;
        public FTransform[] BonePositions; // may be empty (for pre-UE4), position in array matches TrackBoneNames
        public EBoneTranslationRetargetingMode[] BoneModes;
        public readonly List<CAnimSequence> Sequences = new();

        public int BonesCount => TrackBonesInfo.Length;

        public CAnimSet() { }

        public CAnimSet(UObject original)
        {
            OriginalAnim = original;
        }

        /** Make a copy of CAnimSet, except animations */
        public void CopyAllButSequences(CAnimSet other)
        {
            OriginalAnim = other.OriginalAnim;
            TrackBonesInfo = (FMeshBoneInfo[]) other.TrackBonesInfo.Clone();
            BonePositions = (FTransform[]) other.BonePositions.Clone();
            BoneModes = (EBoneTranslationRetargetingMode[]) other.BoneModes.Clone();
        }

        // If Skeleton has at most this number of animations, export them as separate psa files.
        // This is needed because UAnimSequence4 can refer to other animation sequences in properties
        // (e.g. UAnimSequence4::RefPoseSeq).
        //private const int MIN_ANIMSET_SIZE = 4; TODO multiple animations per skeleton

        public UObject GetPrimaryAnimObject()
        {
            // When AnimSet consists of just 1 animation track, it is possible that we're exporting
            // a separate UE4 AnimSequence. In this case it's worth using that AnimSequence's filename,
            // otherwise we'll have multiple animations mapped to the same exported file.
            if (Sequences.Count > 0 && OriginalAnim is USkeleton skeleton)
            {
                /*Trace.Assert(skeleton.OriginalAnims.Count == Sequences.Count);
                // Allow up to 3
                if (skeleton.OriginalAnims.Count <= MIN_ANIMSET_SIZE)
                    return skeleton.OriginalAnims[0];*/
                return Sequences[0].OriginalSequence;
            }

            // Not a Skeleton, or has different animation track count
            return OriginalAnim;
        }
    }
}
