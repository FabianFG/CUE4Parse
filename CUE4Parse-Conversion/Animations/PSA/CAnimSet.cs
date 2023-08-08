using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse_Conversion.Animations.PSA
{
    public class CAnimSet
    {
        public readonly USkeleton Skeleton;
        public readonly List<CAnimSequence> Sequences = new();

        public float TotalAnimTime;

        public CAnimSet(USkeleton skeleton)
        {
            Skeleton = skeleton;
        }
    }
}
