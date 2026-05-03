using System.Collections.Generic;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse_Conversion.V2.Options;

namespace CUE4Parse_Conversion.V2.Formats.Meshes;

public interface IMeshExportFormat : IExportFormat
{
    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExportOptions options, SkeletalMesh dto, IReadOnlyDictionary<string, string>? materialPaths = null);

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExportOptions options, StaticMesh dto, IReadOnlyDictionary<string, string>? materialPaths = null);

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExportOptions options, Skeleton dto);
}
