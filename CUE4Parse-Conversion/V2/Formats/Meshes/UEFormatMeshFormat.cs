using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes.UEFormat;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse_Conversion.V2.Options;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Formats.Meshes;

public sealed class UEFormatMeshFormat : IMeshExportFormat
{
    public string DisplayName => "UEFormat (uemodel)";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExportOptions options, SkeletalMesh dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        using var ar = new FArchiveWriter();
        new UEModel(objectName, dto, options).Save(ar);
        return [new ExportFile("uemodel", ar.GetBuffer())];
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExportOptions options, StaticMesh dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        using var ar = new FArchiveWriter();
        new UEModel(objectName, dto, options).Save(ar);
        return [new ExportFile("uemodel", ar.GetBuffer())];
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExportOptions options, Skeleton dto)
    {
        using var ar = new FArchiveWriter();
        new UEModel(objectName, dto, options).Save(ar);
        return [new ExportFile("uemodel", ar.GetBuffer())];
    }
}

