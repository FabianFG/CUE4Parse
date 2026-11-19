using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.ActorX;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Formats.Meshes;

public sealed class ActorXMeshFormat : IMeshExportFormat
{
    public string DisplayName => "ActorX (psk / pskx)";
    public string Extension => "psk";
    public IReadOnlyList<string> Extensions => ["psk", "pskx"];

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, string objectPath, ExportOptions options, SkeletalMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        var results = new List<ExportFile>();

        foreach (var lod in dto.LODs)
        {
            using var Ar = new FArchiveWriter();
            new ActorXMesh(lod, options).Save(Ar);

            results.Add(new ExportFile(lod.Vertices.Length > 65536 ? Extensions[1] : Extension, Ar.GetBuffer(), lod._suffix));
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, string objectPath, ExportOptions options, StaticMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null)
    {
        var results = new List<ExportFile>();

        foreach (var lod in dto.LODs)
        {
            using var Ar = new FArchiveWriter();
            new ActorXMesh(lod, options).Save(Ar);

            results.Add(new ExportFile(Extensions[1], Ar.GetBuffer(), lod._suffix));
        }

        return results;
    }

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, string objectPath, ExportOptions options, SkeletonDto dto)
    {
        using var Ar = new FArchiveWriter();
        new ActorXMesh(dto, options).Save(Ar);
        return [new ExportFile(Extensions[1], Ar.GetBuffer())];
    }
}
