using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FFrameNumber : IUStruct
    {
        public readonly int Value;

        public override string ToString() => Value.ToString();
    }
}
