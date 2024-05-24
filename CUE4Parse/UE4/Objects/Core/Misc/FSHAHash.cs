using System.Runtime.CompilerServices;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    public readonly struct FSHAHash : IUStruct
    {
        public const int SIZE = 20;

        public readonly byte[] Hash;

        public FSHAHash(FArchive Ar)
        {
            Hash = Ar.ReadBytes(SIZE);
        }

        public FSHAHash(FIoChunkHash InChunkHash)
        {
            Hash = new byte[SIZE];
            Unsafe.CopyBlock(ref Hash[0], ref InChunkHash.Hash[0], (uint) Hash.Length);
        }

        public static implicit operator FSHAHash(FIoChunkHash InChunkHash) => new (InChunkHash);

        public override string ToString()
        {
            unsafe { fixed (byte* ptr = Hash) { return UnsafePrint.BytesToHex(ptr, SIZE); } }
        }
    }
}
