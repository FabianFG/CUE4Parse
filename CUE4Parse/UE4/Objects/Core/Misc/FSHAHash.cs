using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    public readonly struct FSHAHash : IUStruct
    {
        public readonly byte[] Hash;

        public FSHAHash(FArchive Ar)
        {
            Hash = Ar.ReadBytes(20);
        }

        public override string ToString()
        {
            unsafe { fixed (byte* ptr = Hash) { return UnsafePrint.BytesToHex(ptr, 20); } } 
        }
    }
}
