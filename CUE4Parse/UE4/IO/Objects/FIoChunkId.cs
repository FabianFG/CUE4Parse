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

    /// <summary>
    /// Addressable chunk types.
    /// <br/><br/>
    /// The enumerators have explicitly defined values here to encourage backward/forward
    /// compatible updates. 
    /// <br/><br/>
    /// Also note that for certain discriminators, Zen Store will assume certain things
    /// about the structure of the chunk ID so changes must be made carefully.
    /// </summary>
    public enum EIoChunkType5 : byte
    {
        Invalid = 0,
        ExportBundleData = 1,
        BulkData = 2,
        OptionalBulkData = 3,
        MemoryMappedBulkData = 4,
        ScriptObjects = 5,
        ContainerHeader = 6,
        ExternalFile = 7,
        ShaderCodeLibrary = 8,
        ShaderCode = 9,
        PackageStoreEntry = 10
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
#pragma warning disable 660,661
    public readonly struct FIoChunkId
#pragma warning restore 660,661
    {
        public readonly ulong ChunkId;
        public readonly ushort ChunkIndex;
        private readonly byte _padding;
        public readonly byte ChunkType;

        public FIoChunkId(ulong chunkId, ushort chunkIndex, byte chunkType)
        {
            ChunkId = chunkId;
            ChunkIndex = chunkIndex;
            ChunkType = chunkType;
            _padding = 0;
        }

        public FIoChunkId(ulong chunkId, ushort chunkIndex, EIoChunkType chunkType) : this(chunkId, chunkIndex, (byte) chunkType) { }
        public FIoChunkId(ulong chunkId, ushort chunkIndex, EIoChunkType5 chunkType) : this(chunkId, chunkIndex, (byte) chunkType) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FPackageId AsPackageId()
        {
            return new FPackageId(ChunkId);
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