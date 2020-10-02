using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Engine.Curves
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FSimpleCurveKey : IUStruct
    {
        public readonly float KeyTime;
        public readonly float KeyValue;
    }
}
