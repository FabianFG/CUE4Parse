using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Meshes.UEFormat;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Formats.Meshes;

public sealed class UEModelMeshFormat : IMeshExportFormat
{
    public string DisplayName => "UEFormat (uemodel)";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExporterOptions options, USkeletalMesh originalMesh, CSkeletalMesh convertedMesh)
    {
        using var ar = new FArchiveWriter();

        var sockets = new List<FPackageIndex>();
        if (options.SocketFormat != ESocketFormat.None)
        {
            sockets.AddRange(originalMesh.Sockets);
            if (originalMesh.Skeleton.TryLoad<USkeleton>(out var originalSkeleton))
            {
                sockets.AddRange(originalSkeleton.Sockets);
            }
        }

        new UEModel(
            objectName,
            convertedMesh,
            options.ExportMorphTargets ? originalMesh.MorphTargets : null,
            sockets.ToArray(),
            originalMesh.Skeleton,
            originalMesh.PhysicsAsset,
            options
        ).Save(ar);
        return [new ExportFile("uemodel", ar.GetBuffer())];
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExporterOptions options, CStaticMesh convertedMesh)
    {
        using var ar = new FArchiveWriter();
        new UEModel(objectName, convertedMesh, options).Save(ar);
        return [new ExportFile("uemodel", ar.GetBuffer())];
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExporterOptions options, USkeleton skeleton)
    {
        if (!skeleton.TryConvert(out var bones, out _) || bones.Count == 0)
            return [];

        using var ar = new FArchiveWriter();
        new UEModel(objectName, skeleton, bones, skeleton.Sockets, skeleton.VirtualBones, options).Save(ar);
        return [new ExportFile("uemodel", ar.GetBuffer())];
    }
}

