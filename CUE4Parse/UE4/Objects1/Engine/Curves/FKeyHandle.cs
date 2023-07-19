using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Engine.Curves
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FKeyHandle : IUStruct
    {
        public readonly uint Index;
    }
}
