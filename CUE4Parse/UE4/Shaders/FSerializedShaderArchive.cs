using System;
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

        public class FSerializedShaderArchiveConverter : JsonConverter<FSerializedShaderArchive>
        {
            public override void WriteJson(JsonWriter writer, FSerializedShaderArchive value, JsonSerializer serializer)
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

                writer.WritePropertyName("ShaderMapEntries");
                serializer.Serialize(writer, value.ShaderMapEntries);

                writer.WritePropertyName("ShaderEntries");
                serializer.Serialize(writer, value.ShaderEntries);

                writer.WritePropertyName("PreloadEntries");
                serializer.Serialize(writer, value.PreloadEntries);

                writer.WritePropertyName("ShaderIndices");
                serializer.Serialize(writer, value.ShaderIndices);

                writer.WriteEndObject();
            }

            public override FSerializedShaderArchive ReadJson(JsonReader reader, Type objectType, FSerializedShaderArchive existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
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