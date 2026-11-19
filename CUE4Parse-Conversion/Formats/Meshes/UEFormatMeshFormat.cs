using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.UEFormat;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Formats.Meshes;

/// <param name="bNaniteSeparate">UEFormat embed LODs into a single file but you may want the nanite lod written as a separate file</param>
public sealed class UEFormatMeshFormat(bool bNaniteSeparate = false) : IMeshExportFormat
{
    public string DisplayName => "UEFormat (uemodel)";
    public string Extension => "uemodel";

    public IReadOnlyList<ExportFile> BuildSkeletalMesh(string objectName, string objectPath, ExportOptions options, SkeletalMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null)
        => Build(dto.LODs, predicate => new UEModel(objectName, objectPath, dto, options, predicate));

    public IReadOnlyList<ExportFile> BuildStaticMesh(string objectName, string objectPath, ExportOptions options, StaticMeshDto dto, IReadOnlyDictionary<string, string>? materialPaths = null)
        => Build(dto.LODs, predicate => new UEModel(objectName, objectPath, dto, options, predicate));

    public IReadOnlyList<ExportFile> BuildSkeleton(string objectName, string objectPath, ExportOptions options, SkeletonDto dto)
    {
        using var Ar = new FArchiveWriter();
        new UEModel(objectName, objectPath, dto, options).Save(Ar);
        return [new ExportFile(Extension, Ar.GetBuffer())];
    }

    private IReadOnlyList<ExportFile> Build<TVertex>(IList<MeshLodDto<TVertex>> lods, Func<Func<MeshLodDto<TVertex>, bool>?, UEModel> factory) where TVertex : struct, IMeshVertex
    {
        var bHasNanite = false;
        var bHasRegular = false;
        if (bNaniteSeparate)
        {
            foreach (var lod in lods)
            {
                if (lod.IsNanite) bHasNanite = true;
                else bHasRegular = true;

                if (bHasNanite && bHasRegular) break;
            }
        }

        // if full nanite or full regular lods
        if (!bHasNanite || !bHasRegular)
        {
            // file level suffix may disagree with lod level suffix here
            // a single lod dto has no suffix set in SetLodSuffixes()
            return [Save(factory(null), bHasNanite ? "_Nanite" : null)];
        }

        // if both nanite and regular lods
        return [Save(factory(lod => !lod.IsNanite)), Save(factory(lod => lod.IsNanite), "_Nanite")];

        ExportFile Save(UEModel model, string? nameSuffix = null)
        {
            using var Ar = new FArchiveWriter();
            model.Save(Ar);
            return new ExportFile(Extension, Ar.GetBuffer(), nameSuffix);
        }
    }
}
