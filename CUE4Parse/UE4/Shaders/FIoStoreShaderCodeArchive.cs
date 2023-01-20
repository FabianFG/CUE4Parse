using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Shaders
{
    [JsonConverter(typeof(FIoStoreShaderCodeArchiveConverter))]
    public class FIoStoreShaderCodeArchive : FRHIShaderLibrary
    {
        public readonly FSHAHash[] ShaderMapHashes;
        public readonly FSHAHash[] ShaderHashes;
        public readonly FIoChunkId[] ShaderGroupIoHashes;
        public readonly FIoStoreShaderMapEntry[] ShaderMapEntries;
        public readonly FIoStoreShaderCodeEntry[] ShaderEntries;
        public readonly FIoStoreShaderGroupEntry[] ShaderGroupEntries;
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

    public class FIoStoreShaderCodeArchiveConverter : JsonConverter<FIoStoreShaderCodeArchive>
    {
        public override void WriteJson(JsonWriter writer, FIoStoreShaderCodeArchive value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("ShaderMapHashes");
            writer.WriteStartArray();
            foreach (var shaderMapHash in value.ShaderMapHashes)
            {
                serializer.Serialize(writer, shaderMapHash.Hash);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("ShaderHashes");
            writer.WriteStartArray();
            foreach (var shaderHash in value.ShaderHashes)
            {
                serializer.Serialize(writer, shaderHash.Hash);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("ShaderGroupIoHashes");
            serializer.Serialize(writer, value.ShaderGroupIoHashes);

            writer.WritePropertyName("ShaderMapEntries");
            serializer.Serialize(writer, value.ShaderMapEntries);

            writer.WritePropertyName("ShaderEntries");
            serializer.Serialize(writer, value.ShaderEntries);

            writer.WritePropertyName("ShaderGroupEntries");
            serializer.Serialize(writer, value.ShaderGroupEntries);

            writer.WritePropertyName("ShaderIndices");
            serializer.Serialize(writer, value.ShaderIndices);

            writer.WriteEndObject();
        }

        public override FIoStoreShaderCodeArchive ReadJson(JsonReader reader, Type objectType, FIoStoreShaderCodeArchive existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
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
}
