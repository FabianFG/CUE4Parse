using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.glTF;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Formats.Meshes;

public sealed class GltfMeshFormat(bool isObj = false) : IMeshExportFormat
{
    public string DisplayName => isObj ? "Wavefront OBJ" : "glTF 2.0 (binary)";

    private readonly EMeshFormat _legacyFormat = isObj ? EMeshFormat.OBJ : EMeshFormat.Gltf2;
    private readonly string _extension = isObj ? "obj" : "glb";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExporterOptions options, USkeletalMesh originalMesh, CSkeletalMesh convertedMesh)
    {
        var results = new List<ExportFile>();
        var lodIdx = 0;

        for (var i = 0; i < convertedMesh.LODs.Count; i++)
        {
            var lod = convertedMesh.LODs[i];
            if (lod.SkipLod) continue;

            using var ar = new FArchiveWriter();
            new Gltf(
                objectName,
                lod,
                convertedMesh.RefSkeleton,
                materialExports: null,   // materials queued via ExportSession
                options,
                options.ExportMorphTargets ? originalMesh.MorphTargets : null,
                i
            ).Save(_legacyFormat, ar);

            var suffix = lodIdx == 0 ? "" : $"_LOD{lodIdx}";
            results.Add(new ExportFile(_extension, ar.GetBuffer(), suffix));
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
            new Gltf(objectName, lod, materialExports: null, options).Save(_legacyFormat, ar);

            var suffix = lodIdx == 0 ? "" : $"_LOD{lodIdx}";
            results.Add(new ExportFile(_extension, ar.GetBuffer(), suffix));
            lodIdx++;

            if (options.LodFormat == ELodFormat.FirstLod) break;
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExporterOptions options, USkeleton skeleton)
        => throw new NotSupportedException(
            "glTF does not support skeleton-only exports. Please export a skeletal mesh to get a glTF file containing the skeleton.");
}

