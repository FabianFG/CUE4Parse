using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.IO.Objects
{
    public enum FIoStoreTocEntryMetaFlags : byte
    {
        None,
        Compressed		= (1 << 0),
        MemoryMapped	= (1 << 1)
    }

    public readonly struct FIoStoreTocEntryMeta
    {
        public readonly FSHAHash ChunkHash;
        public readonly FIoStoreTocEntryMetaFlags Flags;

        public FIoStoreTocEntryMeta(FArchive Ar, bool replacedIoChunkHashWithIoHash)
        {
            ChunkHash = replacedIoChunkHashWithIoHash ? new FSHAHash(Ar) : new FIoChunkHash(Ar);
            Flags = Ar.Read<FIoStoreTocEntryMetaFlags>();
            if (replacedIoChunkHashWithIoHash) Ar.Position += 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            unsafe
            {
                return $"{Flags} | {UnsafePrint.BytesToHex((byte *)Unsafe.AsPointer(ref Unsafe.AsRef(in ChunkHash.Hash)), 32)}";
            }
        }
    }
}
