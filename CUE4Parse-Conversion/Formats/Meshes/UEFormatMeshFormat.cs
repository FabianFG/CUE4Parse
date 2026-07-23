using System.Collections.Generic;
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
        using var ar = new FArchiveWriter();
        new UEModel(objectName, dto, options).Save(ar);
        return [new ExportFile("uemodel", ar.GetBuffer())];
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExportOptions options, StaticMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        using var ar = new FArchiveWriter();
        new UEModel(objectName, dto, options).Save(ar);
        return [new ExportFile("uemodel", ar.GetBuffer())];
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExportOptions options, SkeletonDto dto)
    {
        using var ar = new FArchiveWriter();
        new UEModel(objectName, dto, options).Save(ar);
        return [new ExportFile("uemodel", ar.GetBuffer())];
    }
}

