using System;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Shaders
{
    public readonly struct FSHA224 : IUStruct
    {
        public const int SIZE = 28;

        public readonly byte[] Hash;

        public FSHA224(FArchive Ar)
        {
            Hash = Ar.ReadBytes(SIZE);
        }

        public override string ToString()
        {
            unsafe { fixed (byte* ptr = Hash) { return UnsafePrint.BytesToHex(ptr, SIZE); } }
        }
    }

    [JsonConverter(typeof(FSerializedShaderArchiveConverter_MarvelRivals))]
    public class FSerializedShaderArchive_MarvelRivals : FRHIShaderLibrary
    {
        public readonly FSHAHash[] ShaderMapHashes;
        public readonly FSHA224[] ShaderHashes;
        public readonly uint Unk;
        public readonly FShaderMapEntry[] ShaderMapEntries;
        public readonly FShaderCodeEntry[] ShaderEntries;
        public readonly FFileCachePreloadEntry[] PreloadEntries;
        public readonly uint[] ShaderIndices;
        // public readonly FHashTable ShaderMapHashTable;
        // public readonly FHashTable ShaderHashTable;

        public FSerializedShaderArchive_MarvelRivals(FArchive Ar)
        {
            ShaderMapHashes = Ar.ReadArray(() => new FSHAHash(Ar));
            ShaderHashes = Ar.ReadArray(() => new FSHA224(Ar));
            Unk = BitConverter.ToUInt32(Ar.ReadBytes(4));
            ShaderMapEntries = Ar.ReadArray<FShaderMapEntry>();
            ShaderEntries = Ar.ReadArray<FShaderCodeEntry>();
            PreloadEntries = Ar.ReadArray<FFileCachePreloadEntry>();
            ShaderIndices = Ar.ReadArray<uint>();
        }
    }
}
