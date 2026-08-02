using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.UEFormat;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Formats.Meshes;

public sealed class UEFormatMeshFormat : IMeshExportFormat
{
    public string DisplayName => "UEFormat (uemodel)";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExportOptions options, SkeletalMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        // if we are exporting nanite as a separate lod AND have a split of nanite lod vs. non-nanite lods
        if (options.NaniteMeshFormat == ENaniteMeshFormat.NaniteSeparate 
            && dto.LODs.Any(l => l.IsNanite) 
            && dto.LODs.Any(l => !l.IsNanite))
        {
            return
            [
                dto.WithLods(lod => !lod.IsNanite, () => Save(objectName, dto, options)),
                dto.WithLods(lod => lod.IsNanite, () => Save(objectName, dto, options, "_Nanite")),
            ];
        }

        return [Save(objectName, dto, options)];
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExportOptions options, StaticMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        if (options.NaniteMeshFormat == ENaniteMeshFormat.NaniteSeparate 
            && dto.LODs.Any(l => l.IsNanite) 
            && dto.LODs.Any(l => !l.IsNanite))
        {
            return
            [
                dto.WithLods(lod => !lod.IsNanite, () => Save(objectName, dto, options)),
                dto.WithLods(lod => lod.IsNanite, () => Save(objectName, dto, options, "_Nanite")),
            ];
        }

        return [Save(objectName, dto, options)];
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExportOptions options, SkeletonDto dto)
    {
        using var ar = new FArchiveWriter();
        new UEModel(objectName, dto, options).Save(ar);
        return [new ExportFile("uemodel", ar.GetBuffer())];
    }

    private static ExportFile Save(string objectName, StaticMeshDto dto, ExportOptions options, string? suffix = null)
    {
        using var ar = new FArchiveWriter();
        new UEModel(objectName, dto, options).Save(ar);
        return new ExportFile("uemodel", ar.GetBuffer(), suffix);
    }

    private static ExportFile Save(string objectName, SkeletalMeshDto dto, ExportOptions options, string? suffix = null)
    {
        using var ar = new FArchiveWriter();
        new UEModel(objectName, dto, options).Save(ar);
        return new ExportFile("uemodel", ar.GetBuffer(), suffix);
    }
}
