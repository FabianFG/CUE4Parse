using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Animations.PSA
{
    public class CAnimSequence
    {
        public string Name; // sequence's name
        public int NumFrames;
        public float Rate;
        public float StartPos;
        public float AnimEndTime;
        public int LoopingCount;
        public List<CAnimTrack> Tracks; // for each CAnimSet.TrackBoneNames
        public bool bAdditive; // used just for on-screen information
        public UAnimSequence OriginalSequence;
        public FTransform[]? RetargetBasePose;

        public CAnimSequence(UAnimSequence originalSequence)
        {
            OriginalSequence = originalSequence;
        }
    }
}
