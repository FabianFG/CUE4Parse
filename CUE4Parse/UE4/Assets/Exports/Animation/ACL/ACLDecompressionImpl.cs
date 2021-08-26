using System.Runtime.CompilerServices;
using CUE4Parse.ACL;

namespace CUE4Parse.UE4.Assets.Exports.Animation.ACL
{
    public static class ACLDecompressionImpl
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SampleRoundingPolicy GetRoundingPolicy(EAnimInterpolationType interpType) => interpType == EAnimInterpolationType.Step ? SampleRoundingPolicy.Floor : SampleRoundingPolicy.None;

        public static void DecompressBone(FAnimSequenceDecompressionContext decompContext, DecompressionContext aclContext, int trackIndex, out FTransform outAtom)
        {
            aclContext.Seek(decompContext.Time, GetRoundingPolicy(decompContext.Interpolation));
            aclContext.DecompressTrack(trackIndex, out outAtom);
        }
    }
}