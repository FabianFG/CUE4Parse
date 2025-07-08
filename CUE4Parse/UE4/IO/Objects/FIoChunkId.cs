using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;

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
        PackageStoreEntry = 10,
        DerivedData = 11,
        EditorDerivedData = 12,
        PackageResource = 13,
        MAX
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FIoChunkId : IEquatable<FIoChunkId>
    {
        public readonly ulong ChunkId;
        public readonly ushort _chunkIndex;
        private readonly byte _padding;
        public readonly byte ChunkType;

        public FIoChunkId(ulong chunkId, ushort chunkIndex, byte chunkType)
        {
            ChunkId = chunkId;
            _chunkIndex = (ushort) ((chunkIndex & 0xFF) << 8 | (chunkIndex & 0xFF00) >> 8); // NETWORK_ORDER16
            ChunkType = chunkType;
            _padding = 0;
        }

        public FIoChunkId(ulong chunkId, ushort chunkIndex, EIoChunkType chunkType) : this(chunkId, chunkIndex, (byte) chunkType) { }
        public FIoChunkId(ulong chunkId, ushort chunkIndex, EIoChunkType5 chunkType) : this(chunkId, chunkIndex, (byte) chunkType) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FPackageId AsPackageId() => new(ChunkId);

        public string GetExtension(IAesVfsReader reader)
        {
            var type = reader.Game >= EGame.GAME_UE5_0 ? typeof(EIoChunkType5) : typeof(EIoChunkType);
            if (Enum.ToObject(type, ChunkType).ToString() is not { } chunkType)
                return ChunkType.ToString();

            return chunkType switch
            {
                "ExportBundleData" when reader is IoStoreReader => "uasset", // umap
                "ExportBundleData" => "uexp",
                "BulkData" => "ubulk",
                "OptionalBulkData" => "uptnl",
                "MemoryMappedBulkData" => "m.ubulk",
                "ShaderCodeLibrary" => "ushaderbytecode",
                "ShaderCode" => "dxbc",
                _ => chunkType
            };
        }

        public unsafe ulong HashWithSeed(int seed)
        {
            fixed (FIoChunkId* ptr = &this)
            {
                var dataSize = sizeof(FIoChunkId);
                var hash = seed != 0 ? (ulong) seed : 0xcbf29ce484222325;
                for (var index = 0; index < dataSize; ++index)
                {
                    hash = (hash * 0x00000100000001B3) ^ ((byte*) ptr)[index];
                }
                return hash;
            }
        }

        public static bool operator ==(FIoChunkId left, FIoChunkId right) => left.Equals(right);

        public static bool operator !=(FIoChunkId left, FIoChunkId right) => !left.Equals(right);

        public bool Equals(FIoChunkId other) => ChunkId == other.ChunkId && ChunkType == other.ChunkType;

        public override bool Equals(object? obj) => obj is FIoChunkId other && Equals(other);

        public override unsafe int GetHashCode()
        {
            fixed (FIoChunkId* ptr = &this)
            {
                var dataSize = sizeof(FIoChunkId);
                var hash = 5381;
                for (int i = 0; i < dataSize; ++i)
                {
                    hash = hash * 33 + ((byte*) ptr)[i];
                }
                return hash;
            }
        }

        public override string ToString() => $"0x{ChunkId:X8} | {ChunkType}";
    }
}
