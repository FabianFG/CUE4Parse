using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FSkinWeightOverrideInfo
    {
        public readonly uint InfluencesOffset;
        public readonly byte NumInfluences;
    }
}