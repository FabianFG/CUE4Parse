using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Shaders
{
    [JsonConverter(typeof(FSerializedShaderArchiveConverter))]
    public class FSerializedShaderArchive : FRHIShaderLibrary
    {
        public readonly FSHAHash[] ShaderMapHashes;
        public readonly FSHAHash[] ShaderHashes;
        public readonly FShaderMapEntry[] ShaderMapEntries;
        public readonly FShaderCodeEntry[] ShaderEntries;
        public readonly FFileCachePreloadEntry[] PreloadEntries;
        public readonly uint[] ShaderIndices;
        // public readonly FHashTable ShaderMapHashTable;
        // public readonly FHashTable ShaderHashTable;

        public FSerializedShaderArchive(FArchive Ar)
        {
            ShaderMapHashes = Ar.ReadArray(() => new FSHAHash(Ar));
            ShaderHashes = Ar.ReadArray(() => new FSHAHash(Ar));
            ShaderMapEntries = Ar.ReadArray<FShaderMapEntry>();
            ShaderEntries = Ar.ReadArray<FShaderCodeEntry>();
            PreloadEntries = Ar.ReadArray<FFileCachePreloadEntry>();
            ShaderIndices = Ar.ReadArray<uint>();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FShaderMapEntry
    {
        public readonly uint ShaderIndicesOffset;
        public readonly uint NumShaders;
        public readonly uint FirstPreloadIndex;
        public readonly uint NumPreloadEntries;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FShaderCodeEntry
    {
        public readonly ulong Offset;
        public readonly uint Size;
        public readonly uint UncompressedSize;
        public readonly byte Frequency;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FFileCachePreloadEntry
    {
        public readonly long Offset;
        public readonly long Size;
    }
}
