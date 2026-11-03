using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.Gltf;

namespace CUE4Parse_Conversion.Formats.Meshes;

public sealed class GltfMeshFormat(ExportSession? session = null) : IMeshExportFormat
{
    public string DisplayName => "glTF 2.0 (binary)";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, string objectPath, ExportOptions options, SkeletalMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        var results = new List<ExportFile>();
        var materials = new GltfMaterialExporter(options, session ?? new ExportSession(options));

        foreach (var lod in dto.LODs)
        {
            var data = new Gltf(objectName, lod, options, materials).Model.WriteGLB().ToArray();
            results.Add(new ExportFile("glb", data, lod._suffix));
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, string objectPath, ExportOptions options, StaticMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        var results = new List<ExportFile>();
        var materials = new GltfMaterialExporter(options, session ?? new ExportSession(options));

        foreach (var lod in dto.LODs)
        {
            var data = new Gltf(objectName, lod, options, materials).Model.WriteGLB().ToArray();
            results.Add(new ExportFile("glb", data, lod._suffix));
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, string objectPath, ExportOptions options, SkeletonDto dto)
        => throw new NotSupportedException("glTF does not support skeleton-only exports. Please export a skeletal mesh to get a glTF file containing the skeleton.");
}

