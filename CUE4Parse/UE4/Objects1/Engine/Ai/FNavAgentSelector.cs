using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Engine.Ai
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FNavAgentSelector : IUStruct
    {
        public readonly uint PackedBits;
    }
}
