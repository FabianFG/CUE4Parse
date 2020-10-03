using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Pak.Reader;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    public readonly struct FSHAHash : IUStruct
    {
        public readonly byte[] Hash;

        public FSHAHash(FPakArchive Ar)
        {
            Hash = Ar.ReadBytes(20);
        }

        public override string ToString()
        {
            unsafe { return UnsafePrint.BytesToHex((byte*) Unsafe.AsPointer(ref Hash[0]), 20); }
        }
    }
}
