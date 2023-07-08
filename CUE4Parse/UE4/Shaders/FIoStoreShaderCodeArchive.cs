using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Shaders;

[JsonConverter(typeof(FIoStoreShaderCodeArchiveConverter))]
public class FIoStoreShaderCodeArchive(FArchive Ar) : FRHIShaderLibrary
{
    public readonly FSHAHash[] ShaderMapHashes = Ar.ReadArray(() => new FSHAHash(Ar));
    public readonly FSHAHash[] ShaderHashes = Ar.ReadArray(() => new FSHAHash(Ar));
    public readonly FIoChunkId[] ShaderGroupIoHashes = Ar.ReadArray<FIoChunkId>();
    public readonly FIoStoreShaderMapEntry[] ShaderMapEntries = Ar.ReadArray<FIoStoreShaderMapEntry>();
    public readonly FIoStoreShaderCodeEntry[] ShaderEntries = Ar.ReadArray<FIoStoreShaderCodeEntry>();
    public readonly FIoStoreShaderGroupEntry[] ShaderGroupEntries = Ar.ReadArray<FIoStoreShaderGroupEntry>();

    public readonly uint[] ShaderIndices = Ar.ReadArray<uint>();
    // public readonly FHashTable ShaderMapHashTable;
    // public readonly FHashTable ShaderHashTable;
    // public readonly FShaderPreloadEntry[] ShaderPreloads;
    // public readonly FRWLock ShaderPreloadLock;
}

public class FIoStoreShaderCodeArchiveConverter : JsonConverter<FIoStoreShaderCodeArchive>
{
    public override void WriteJson(JsonWriter writer, FIoStoreShaderCodeArchive? value, JsonSerializer serializer)
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

        writer.WritePropertyName("ShaderGroupIoHashes");
        serializer.Serialize(writer, value?.ShaderGroupIoHashes);

        writer.WritePropertyName("ShaderMapEntries");
        serializer.Serialize(writer, value?.ShaderMapEntries);

        writer.WritePropertyName("ShaderEntries");
        serializer.Serialize(writer, value?.ShaderEntries);

        writer.WritePropertyName("ShaderGroupEntries");
        serializer.Serialize(writer, value?.ShaderGroupEntries);

        writer.WritePropertyName("ShaderIndices");
        serializer.Serialize(writer, value?.ShaderIndices);

        writer.WriteEndObject();
    }

    public override FIoStoreShaderCodeArchive ReadJson(JsonReader reader, Type objectType, FIoStoreShaderCodeArchive? existingValue, bool hasExistingValue, JsonSerializer serializer)
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
