using System.Collections.Generic;
using CUE4Parse_Conversion.V2.Dto;

namespace CUE4Parse_Conversion.V2.Formats.Meshes;

public interface IMeshExportFormat : IExportFormat
{
    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExporterOptions options, SkeletalMesh dto);

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExporterOptions options, StaticMesh dto);

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExporterOptions options, Skeleton dto);
}

