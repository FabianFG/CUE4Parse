using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Shaders;

[JsonConverter(typeof(FPipelineCacheFileConverter))]
public sealed class FPipelineCacheFile
{
    const ulong FPipelineCacheFileFormatMagic = 0x5049504543414348; // PIPECACH

    public readonly FPipelineCacheFileFormatHeader Header;
    public readonly Dictionary<uint, FPipelineCacheFileFormatPSO> PSOs;
    public readonly FPipelineCacheFileFormatTOC TOC;

    public FPipelineCacheFile(FArchive Ar)
    {
        Header = new FPipelineCacheFileFormatHeader(Ar);
        if (Header.Magic != FPipelineCacheFileFormatMagic || Header.TableOffset < 0 || Header.TableOffset > (ulong)Ar.Length)
            throw new ParserException("Invalid or unsupported PIPECACH file type!");

        // TOC is assumed to be at the end of the file for lates formats
        Ar.Position = (long) Header.TableOffset;
        TOC = new FPipelineCacheFileFormatTOC(Ar, Header.Version);

        PSOs = new Dictionary<uint, FPipelineCacheFileFormatPSO>(TOC.MetaData.Count);
        foreach (var tocEntry in TOC.MetaData)
        {
            Ar.Position = (long)tocEntry.Value.FileOffset;
            PSOs[tocEntry.Key] = new FPipelineCacheFileFormatPSO(Ar, Header.Version);
            if ((ulong)(Ar.Position - (long)tocEntry.Value.FileOffset) != tocEntry.Value.FileSize)
            {
                throw new ParserException("PSO size mismatch!");
            }
        }
    }
}

public struct FPipelineCacheFileFormatHeader
{
    public ulong Magic;
    public EPipelineCacheFileFormatVersions Version;
    public uint GameVersion;
    public EShaderPlatform Platform;
    public FGuid Guid;
    public ulong TableOffset;
    public ulong? LastGCUnixTime;   // Missing in older versions

    public FPipelineCacheFileFormatHeader(FArchive Ar)
    {
        Magic = Ar.Read<ulong>();
        Version = Ar.Read<EPipelineCacheFileFormatVersions>();
        GameVersion = Ar.Read<uint>();
        Platform = Ar.Read<EShaderPlatform>();
        Guid = Ar.Read<FGuid>();
        TableOffset = Ar.Read<ulong>();
        if (Version >= EPipelineCacheFileFormatVersions.LastUsedTime)
            LastGCUnixTime = Ar.Read<ulong>();
    }
}

public enum EPipelineCacheFileFormatVersions : uint
{
    FirstWorking = 7,
    LibraryID = 9,
    ShaderMetaData = 10,
    SortedVertexDesc = 11,
    TOCMagicGuard = 12,
    PSOUsageMask = 13,
    PSOBindCount = 14,
    EOFMarker = 15,
    EngineFlags = 16,
    Subpass = 17,
    PatchSizeReduction_NoDuplicatedGuid = 18,
    AlphaToCoverage = 19,
    AddingMeshShaders = 20,
    RemovingTessellationShaders = 21,
    LastUsedTime = 22,
    MoreRenderTargetFlags = 23,
    FragmentDensityAttachment = 24,
    AddingDepthClipMode = 25,
    BeforeStableCacheVersioning = 26,
    RemovingLineAA = 27,
    AddingDepthBounds = 28,
}

public enum PSOOrder : uint
{
    Default = 0,
    FirstToLatestUsed = 1,
    MostToLeastUsed = 2
}

public class FPipelineCacheFileConverter : JsonConverter<FPipelineCacheFile>
{
    public override void WriteJson(JsonWriter writer, FPipelineCacheFile value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.Header));
        serializer.Serialize(writer, value.Header);
        writer.WritePropertyName(nameof(value.TOC));
        serializer.Serialize(writer, value.TOC);

        //writer.WritePropertyName(nameof(value.PSOs));
        //serializer.Serialize(writer, value.PSOs);

        writer.WriteEndObject();
    }

    public override FPipelineCacheFile ReadJson(JsonReader reader, Type objectType, FPipelineCacheFile existingValue, bool hasExistingValue,
        JsonSerializer serializer)
        => throw new NotImplementedException();
}
