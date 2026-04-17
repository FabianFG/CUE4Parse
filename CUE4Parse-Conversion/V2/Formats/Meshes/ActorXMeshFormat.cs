using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Formats.Meshes;

public sealed class ActorXMeshFormat : IMeshExportFormat
{
    public string DisplayName => "ActorX (psk / pskx)";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExporterOptions options, USkeletalMesh originalMesh, CSkeletalMesh convertedMesh)
    {
        var results = new List<ExportFile>();
        var lodIdx = 0;

        var morphTargets = options.ExportMorphTargets ? originalMesh.MorphTargets : null;

        var sockets = new List<FPackageIndex>();
        if (options.SocketFormat != ESocketFormat.None)
        {
            sockets.AddRange(originalMesh.Sockets);
            if (originalMesh.Skeleton.TryLoad<USkeleton>(out var originalSkeleton))
            {
                sockets.AddRange(originalSkeleton.Sockets);
            }
        }

        for (var i = 0; i < convertedMesh.LODs.Count; i++)
        {
            var lod = convertedMesh.LODs[i];
            if (lod.SkipLod) continue;

            using var ar = new FArchiveWriter();
            new ActorXMesh(
                lod,
                convertedMesh.RefSkeleton,
                materialExports: null,   // materials are queued via ExportSession, not collected here
                morphTargets,
                sockets.ToArray(),
                i,
                options
            ).Save(ar);

            var suffix = lodIdx == 0 ? "" : $"_LOD{lodIdx}";
            results.Add(new ExportFile(lod.NumVerts > 65536 ? "pskx" : "psk", ar.GetBuffer(), suffix));
            lodIdx++;

            if (options.LodFormat == ELodFormat.FirstLod) break;
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExporterOptions options, CStaticMesh convertedMesh)
    {
        var results = new List<ExportFile>();
        var lodIdx = 0;

        foreach (var lod in convertedMesh.LODs)
        {
            if (lod.SkipLod) continue;

            using var ar = new FArchiveWriter();
            new ActorXMesh(lod, materialExports: null, convertedMesh.Sockets ?? [], options).Save(ar);

            var suffix = lodIdx == 0 ? "" : $"_LOD{lodIdx}";
            results.Add(new ExportFile("pskx", ar.GetBuffer(), suffix));
            lodIdx++;

            if (options.LodFormat == ELodFormat.FirstLod) break;
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExporterOptions options, USkeleton skeleton)
    {
        if (!skeleton.TryConvert(out var bones, out _) || bones.Count == 0)
            return [];

        using var ar = new FArchiveWriter();
        new ActorXMesh(bones, skeleton.Sockets, options).Save(ar);
        return [new ExportFile("pskx", ar.GetBuffer())];
    }
}

