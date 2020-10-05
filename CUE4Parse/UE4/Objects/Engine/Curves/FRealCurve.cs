using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Engine.Curves
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRealCurve : IUStruct
    {
        public readonly float DefaultValue;
        public readonly ERichCurveExtrapolation PreInfinityExtrap;
        public readonly ERichCurveExtrapolation PostInfinityExtrap;
    }
}
