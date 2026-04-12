using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.V2.Formats.Meshes;

public interface IMeshExportFormat : IExportFormat
{
    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExporterOptions options, USkeletalMesh originalMesh, CSkeletalMesh convertedMesh, FPackageIndex[] sockets);

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExporterOptions options, UStaticMesh originalMesh, CStaticMesh convertedMesh);

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExporterOptions options, USkeleton skeleton);
}

