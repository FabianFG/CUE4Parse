using System;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FDateTime : IUStruct
    {
        public readonly long Ticks;

        public override string ToString() => $"{new DateTime(Ticks):F}";
    }
}
