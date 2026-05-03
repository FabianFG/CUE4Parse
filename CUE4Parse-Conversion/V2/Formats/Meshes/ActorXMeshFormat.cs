using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Formats.Meshes;

public sealed class ActorXMeshFormat : IMeshExportFormat
{
    public string DisplayName => "ActorX (psk / pskx)";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExporterOptions options, SkeletalMesh dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        var results = new List<ExportFile>();
        var lodIdx = 0;

        for (var i = 0; i < dto.LODs.Count; i++)
        {
            var lod = dto.LODs[i];
            using var ar = new FArchiveWriter();
            new ActorXMesh(dto, options, i).Save(ar);

            var suffix = lodIdx == 0 ? "" : $"_LOD{lodIdx}";
            results.Add(new ExportFile(lod.Vertices.Length > 65536 ? "pskx" : "psk", ar.GetBuffer(), suffix));
            lodIdx++;

            if (options.LodFormat == ELodFormat.FirstLod) break;
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExporterOptions options, StaticMesh dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        var results = new List<ExportFile>();
        var lodIdx = 0;

        for (var i = 0; i < dto.LODs.Count; i++)
        {
            var lod = dto.LODs[i];
            using var ar = new FArchiveWriter();
            new ActorXMesh(dto, options, i).Save(ar);

            var suffix = lodIdx == 0 ? "" : $"_LOD{lodIdx}";
            results.Add(new ExportFile("pskx", ar.GetBuffer(), suffix));
            lodIdx++;

            if (options.LodFormat == ELodFormat.FirstLod) break;
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExporterOptions options, Skeleton dto)
    {
        using var ar = new FArchiveWriter();
        new ActorXMesh(dto, options).Save(ar);
        return [new ExportFile("pskx", ar.GetBuffer())];
    }
}

