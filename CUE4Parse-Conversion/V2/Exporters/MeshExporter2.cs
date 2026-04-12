using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.V2.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class MeshExporter2 : ExporterBase2
{
    private readonly UObject? _mesh;

    public MeshExporter2(USkeletalMesh skeletalMesh) : base(skeletalMesh)
    {
        _mesh = skeletalMesh;
    }

    public MeshExporter2(UStaticMesh staticMesh) : base(staticMesh)
    {
        _mesh = staticMesh;
    }

    public MeshExporter2(USkeleton skeleton) : base(skeleton)
    {
        _mesh = skeleton;
    }

    public override async Task<IReadOnlyList<ExportResult>> ExportAsync(IProgress<ExportProgress>? progress = null, CancellationToken ct = default)
    {
        Log.Debug("Converting mesh to {Format}", Session.Options.MeshFormat);

        IReadOnlyList<ExportFile> files;
        var format = GetMeshFormat(Session.Options.MeshFormat);

        switch (_mesh)
        {
            case USkeletalMesh skeletalMesh:
            {
                if (!skeletalMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count == 0)
                {
                    return [ExportResult.Failure(ObjectName, PackagePath, PackageDirectory, new Exception("Failed to convert skeletal mesh or no LODs"))];
                }

                var sockets = skeletalMesh.Skeleton.TryLoad<USkeleton>(out var skeleton) ? skeleton.Sockets : [];
                files = format.BuildSkeletalMesh(ObjectName, Session.Options, skeletalMesh, convertedMesh, sockets);

                if (Session.Options.ExportMaterials)
                {
                    foreach (var ptr in skeletalMesh.Materials)
                    {
                        if (ptr?.TryLoad<UMaterialInterface>(out var material) == true)
                        {
                            Session.TryEnqueue(new MaterialExporter3(material));
                        }
                    }
                }
                break;
            }
            case UStaticMesh staticMesh:
            {
                if (!staticMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count == 0)
                {
                    return [ExportResult.Failure(ObjectName, PackagePath, PackageDirectory, new Exception("Failed to convert static mesh or no LODs"))];
                }

                files = format.BuildStaticMesh(ObjectName, Session.Options, staticMesh, convertedMesh);

                if (Session.Options.ExportMaterials)
                {
                    foreach (var ptr in staticMesh.Materials)
                    {
                        if (ptr?.TryLoad<UMaterialInterface>(out var material) == true)
                        {
                            Session.TryEnqueue(new MaterialExporter3(material));
                        }
                    }
                }

                break;
            }
            case USkeleton skeleton:
            {
                files = format.BuildSkeleton(ObjectName, Session.Options, skeleton);
                break;
            }
            default:
                return [ExportResult.Failure(ObjectName, PackagePath, PackageDirectory, new Exception("Unsupported mesh type"))];
        }

        if (files.Count == 0)
        {
            return [ExportResult.Failure(ObjectName, PackagePath, PackageDirectory, new Exception("Format produced no files"))];
        }

        var tasks = files.Select(file => WriteExportFileAsync(file, progress, ct));
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }

    private IMeshExportFormat GetMeshFormat(EMeshFormat format) => format switch
    {
        EMeshFormat.ActorX => new ActorXMeshFormat(),
        EMeshFormat.Gltf2 => new GltfMeshFormat(isObj: false),
        EMeshFormat.OBJ => new GltfMeshFormat(isObj: true),
        EMeshFormat.UEFormat => new UEModelMeshFormat(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported mesh format")
    };
}
