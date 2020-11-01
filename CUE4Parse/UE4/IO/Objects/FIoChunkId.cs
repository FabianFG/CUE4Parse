using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.IO.Objects
{
    /// <summary>
    /// Addressable chunk types.
    /// </summary>
    public enum EIoChunkType : byte
    {
        Invalid,
        InstallManifest,
        ExportBundleData,
        BulkData,
        OptionalBulkData,
        MemoryMappedBulkData,
        LoaderGlobalMeta,
        LoaderInitialLoadMeta,
        LoaderGlobalNames,
        LoaderGlobalNameHashes,
        ContainerHeader
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FIoChunkId
    {
        public readonly ulong ChunkId;
        public readonly ushort ChunkIndex;
        private readonly byte _padding;
        public readonly EIoChunkType ChunkType;

        public FIoChunkId(ulong chunkId, ushort chunkIndex, EIoChunkType chunkType)
        {
            ChunkId = chunkId;
            ChunkIndex = chunkIndex;
            ChunkType = chunkType;
            _padding = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FIoChunkId a, FIoChunkId b) => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FIoChunkId a, FIoChunkId b) => !a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            unsafe
            {
                return UnsafePrint.BytesToHex(
                    (byte*) Unsafe.AsPointer(ref Unsafe.AsRef(in this)), 12);
            }
        }
    }
}