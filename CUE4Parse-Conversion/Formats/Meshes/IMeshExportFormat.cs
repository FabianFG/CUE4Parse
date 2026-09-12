using System.Collections.Generic;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;

namespace CUE4Parse_Conversion.Formats.Meshes;

public interface IMeshExportFormat : IExportFormat
{
    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, string objectPath, ExportOptions options, SkeletalMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null);

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, string objectPath, ExportOptions options, StaticMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null);

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, string objectPath, ExportOptions options, SkeletonDto dto);
}
