using System.Collections.Generic;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse_Conversion.V2.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class SkeletonExporter(USkeleton originalSkeleton) : MeshExporter2<USkeleton>(originalSkeleton)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(USkeleton originalSkeleton, IMeshExportFormat format)
    {
        return format.BuildSkeleton(ObjectName, Session.Options, new Skeleton(originalSkeleton));
    }
}
