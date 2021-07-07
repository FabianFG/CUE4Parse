using System;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Shaders
{
    [JsonConverter(typeof(FSerializedShaderArchiveConverter))]
    public class FSerializedShaderArchive
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
            ShaderMapEntries = Ar.ReadArray(() => new FShaderMapEntry(Ar));
            ShaderEntries = Ar.ReadArray(() => new FShaderCodeEntry(Ar));
            PreloadEntries = Ar.ReadArray(() => new FFileCachePreloadEntry(Ar));
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

    public class FShaderMapEntry
    {
        public readonly uint ShaderIndecesOffset;
        public readonly uint NumShaders;
        public readonly uint FirstPreloadIndex;
        public readonly uint NumPreloadEntries;

        public FShaderMapEntry(FArchive Ar)
        {
            ShaderIndecesOffset = Ar.Read<uint>();
            NumShaders = Ar.Read<uint>();
            FirstPreloadIndex = Ar.Read<uint>();
            NumPreloadEntries = Ar.Read<uint>();
        }
    }

    public class FShaderCodeEntry
    {
        public readonly ulong Offset;
        public readonly uint Size;
        public readonly uint UncompressedSize;
        public readonly byte Frequency;

        public FShaderCodeEntry(FArchive Ar)
        {
            Offset = Ar.Read<ulong>();
            Size = Ar.Read<uint>();
            UncompressedSize = Ar.Read<uint>();
            Frequency = Ar.Read<byte>();
        }
    }

    public class FFileCachePreloadEntry
    {
        public readonly long Offset;
        public readonly long Size;

        public FFileCachePreloadEntry(FArchive Ar)
        {
            Offset = Ar.Read<long>();
            Size = Ar.Read<long>();
        }
    }
}