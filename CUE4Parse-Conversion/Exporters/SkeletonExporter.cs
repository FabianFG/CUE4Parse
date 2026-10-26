using System.Collections.Generic;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse_Conversion.Exporters;

public sealed class SkeletonExporter(USkeleton originalSkeleton) : MeshExporter<USkeleton>(originalSkeleton)
{
    // Skeletons have no materials, so a deferred skeleton doesn't need to be loaded to collect them
    internal override bool HasDependencies => false;

    protected override IReadOnlyList<ExportFile> BuildFiles(USkeleton originalSkeleton, IMeshExportFormat format)
    {
        using var dto = new SkeletonDto(originalSkeleton);
        return format.BuildSkeleton(ObjectName, ObjectPath, Session.Options, dto);
    }
}
