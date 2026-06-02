using System.Runtime.InteropServices;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Shaders;

[JsonConverter(typeof(FIoStoreShaderCodeArchiveConverter))]
public class FIoStoreShaderCodeArchive : FRHIShaderLibrary
{
    /** Hashes of all shadermaps in the library */
    public readonly FSHAHash[] ShaderMapHashes;
    /** Output hashes of all shaders in the library */
    public readonly FSHAHash[] ShaderHashes;
    /** Chunk Ids (essentially hashes) of the shader groups - needed to be serialized as they are used for preloading. */
    public readonly FIoChunkId[] ShaderGroupIoHashes;
    /** An array of a shadermap descriptors. Each shadermap can reference an arbitrary number of shaders */
    public readonly FIoStoreShaderMapEntry[] ShaderMapEntries;
    /** An array of all shaders descriptors, deduplicated */
    public readonly FIoStoreShaderCodeEntry[] ShaderEntries;
    /** An array of shader group descriptors */
    public readonly FIoStoreShaderGroupEntry[] ShaderGroupEntries;
    /** Flat array of shaders referenced by all shadermaps. Each shadermap has a range in this array, beginning of which is
      * stored as ShaderIndicesOffset in the shadermap's descriptor (FIoStoreShaderMapEntry).
      * This is also used by the shader groups. */
    public readonly uint[] ShaderIndices;
    // public readonly FHashTable ShaderMapHashTable;
    // public readonly FHashTable ShaderHashTable;
    // public readonly FShaderPreloadEntry[] ShaderPreloads;
    // public readonly FRWLock ShaderPreloadLock;

    public FIoStoreShaderCodeArchive(FArchive Ar)
    {
        ShaderMapHashes = Ar.ReadArray(() => new FSHAHash(Ar));
        ShaderHashes = Ar.ReadArray(() => new FSHAHash(Ar));
        ShaderGroupIoHashes = Ar.ReadArray<FIoChunkId>();
        ShaderMapEntries = Ar.ReadArray<FIoStoreShaderMapEntry>();
        ShaderEntries = Ar.ReadArray<FIoStoreShaderCodeEntry>();
        ShaderGroupEntries = Ar.ReadArray<FIoStoreShaderGroupEntry>();
        ShaderIndices = Ar.ReadArray<uint>();
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FIoStoreShaderMapEntry
{
    public readonly uint ShaderIndicesOffset;
    public readonly uint NumShaders;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FIoStoreShaderCodeEntry
{
    public long Frequency => Packed & 0xf;
    public long ShaderGroupIndex => (Packed & 0x3FFFFFFF0) >> 4;
    public long UncompressedOffsetInGroup => Packed >> 34;

    public readonly long Packed;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FIoStoreShaderGroupEntry
{
    public readonly uint ShaderIndicesOffset;
    public readonly uint NumShaders;
    public readonly uint UncompressedSize;
    public readonly uint CompressedSize;
}
