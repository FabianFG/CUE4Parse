using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Shaders;

[JsonConverter(typeof(FSerializedShaderArchiveConverter))]
public class FSerializedShaderArchive(FArchive Ar) : FRHIShaderLibrary
{
    public readonly FSHAHash[] ShaderMapHashes = Ar.ReadArray(() => new FSHAHash(Ar));
    public readonly FSHAHash[] ShaderHashes = Ar.ReadArray(() => new FSHAHash(Ar));
    public readonly FShaderMapEntry[] ShaderMapEntries = Ar.ReadArray<FShaderMapEntry>();
    public readonly FShaderCodeEntry[] ShaderEntries = Ar.ReadArray<FShaderCodeEntry>();
    public readonly FFileCachePreloadEntry[] PreloadEntries = Ar.ReadArray<FFileCachePreloadEntry>();

    public readonly uint[] ShaderIndices = Ar.ReadArray<uint>();
    // public readonly FHashTable ShaderMapHashTable;
    // public readonly FHashTable ShaderHashTable;

    public class FSerializedShaderArchiveConverter : JsonConverter<FSerializedShaderArchive>
    {
        public override void WriteJson(JsonWriter writer, FSerializedShaderArchive? value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            if (value?.ShaderMapHashes.Length > 0)
            {
                writer.WritePropertyName("ShaderMapHashes");
                writer.WriteStartArray();
                foreach (var shaderMapHash in value.ShaderMapHashes)
                {
                    serializer.Serialize(writer, shaderMapHash.Hash);
                }

                writer.WriteEndArray();
            }

            if (value?.ShaderHashes.Length > 0)
            {
                writer.WritePropertyName("ShaderHashes");
                writer.WriteStartArray();
                foreach (var shaderHash in value.ShaderHashes)
                {
                    serializer.Serialize(writer, shaderHash.Hash);
                }

                writer.WriteEndArray();
            }

            writer.WritePropertyName("ShaderMapEntries");
            serializer.Serialize(writer, value?.ShaderMapEntries);

            writer.WritePropertyName("ShaderEntries");
            serializer.Serialize(writer, value?.ShaderEntries);

            writer.WritePropertyName("PreloadEntries");
            serializer.Serialize(writer, value?.PreloadEntries);

            writer.WritePropertyName("ShaderIndices");
            serializer.Serialize(writer, value?.ShaderIndices);

            writer.WriteEndObject();
        }

        public override FSerializedShaderArchive ReadJson(JsonReader reader, Type objectType, FSerializedShaderArchive? existingValue, bool hasExistingValue, JsonSerializer serializer)
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
