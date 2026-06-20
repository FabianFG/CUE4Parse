using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.Gltf;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Formats.Meshes;

public sealed class GltfMeshFormat : IMeshExportFormat
{
    public string DisplayName => "glTF 2.0 (binary)";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, ExportOptions options, SkeletalMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        var results = new List<ExportFile>();

        var (start, end) = options.MeshQuality.GetRange(dto.LODs.Count);
        for (var i = start; i < end; i++)
        {
            using var ar = new FArchiveWriter();
            new Gltf(objectName, dto, i, options.ExportMorphTargets).Save(ar);

            var suffix = i == 0 ? "" : $"_LOD{i}";
            results.Add(new ExportFile("glb", ar.GetBuffer(), suffix));
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, ExportOptions options, StaticMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        var results = new List<ExportFile>();

        var (start, end) = options.MeshQuality.GetRange(dto.LODs.Count);
        for (var i = start; i < end; i++)
        {
            using var ar = new FArchiveWriter();
            new Gltf(objectName, dto, i).Save(ar);

            var suffix = i == 0 ? "" : $"_LOD{i}";
            results.Add(new ExportFile("glb", ar.GetBuffer(), suffix));
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, ExportOptions options, SkeletonDto dto)
        => throw new NotSupportedException(
            "glTF does not support skeleton-only exports. Please export a skeletal mesh to get a glTF file containing the skeleton.");
}

