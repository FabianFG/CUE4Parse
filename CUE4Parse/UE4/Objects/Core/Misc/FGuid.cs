using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FGuid
    {
        public readonly uint A;
        public readonly uint B;
        public readonly uint C;
        public readonly uint D;
        
        public unsafe string HexString => UnsafePrint.BytesToHex(
            (byte*) Unsafe.AsPointer(ref this), 16);
    }
}