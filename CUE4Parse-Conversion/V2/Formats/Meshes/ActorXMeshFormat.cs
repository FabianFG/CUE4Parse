using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse_Conversion.V2.Options;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Formats.Meshes;

public sealed class ActorXMeshFormat : IMeshExportFormat
{
    public string DisplayName => "ActorX (psk / pskx)";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExportOptions options, SkeletalMesh dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        var results = new List<ExportFile>();

        var (start, end) = options.MeshQuality.GetRange(dto.LODs.Count);
        for (var i = start; i < end; i++)
        {
            using var ar = new FArchiveWriter();
            new ActorXMesh(dto, options, i).Save(ar);

            var suffix = i == 0 ? "" : $"_LOD{i}";
            results.Add(new ExportFile(dto.LODs[i].Vertices.Length > 65536 ? "pskx" : "psk", ar.GetBuffer(), suffix));
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExportOptions options, StaticMesh dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        var results = new List<ExportFile>();

        var (start, end) = options.MeshQuality.GetRange(dto.LODs.Count);
        for (var i = start; i < end; i++)
        {
            using var ar = new FArchiveWriter();
            new ActorXMesh(dto, options, i).Save(ar);

            var suffix = i == 0 ? "" : $"_LOD{i}";
            results.Add(new ExportFile("pskx", ar.GetBuffer(), suffix));
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExportOptions options, Skeleton dto)
    {
        using var ar = new FArchiveWriter();
        new ActorXMesh(dto, options).Save(ar);
        return [new ExportFile("pskx", ar.GetBuffer())];
    }
}

