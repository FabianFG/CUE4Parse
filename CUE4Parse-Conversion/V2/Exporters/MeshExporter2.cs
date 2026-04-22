using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.V2.Formats.Meshes;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;

namespace CUE4Parse_Conversion.V2.Exporters;

public abstract class MeshExporter2<T>(T mesh) : ExporterBase2(mesh) where T : UObject
{
    protected abstract IReadOnlyList<ExportFile> BuildFiles(T original, IMeshExportFormat format);

    protected sealed override async Task<IReadOnlyList<ExportResult>> DoExportAsync(CancellationToken ct = default)
    {
        Log.Debug("Converting mesh to {Format}", Session.Options.MeshFormat);

        var files = BuildFiles(mesh, GetMeshFormat(Session.Options.MeshFormat));
        if (files.Count == 0)
        {
            throw new Exception("Format produced no files");
        }

        var tasks = files.Select(file => WriteExportFileAsync(file, ct));
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }

    protected void EnqueueMaterials(params ResolvedObject?[] materials)
    {
        foreach (var ptr in materials)
        {
            if (ptr?.TryLoad<UMaterialInterface>(out var material) == true)
            {
                Session.Add(new MaterialExporter3(material));
            }
        }
    }

    private IMeshExportFormat GetMeshFormat(EMeshFormat format) => format switch
    {
        EMeshFormat.ActorX => new ActorXMeshFormat(),
        EMeshFormat.Gltf2 => new GltfMeshFormat(isObj: false),
        EMeshFormat.OBJ => new GltfMeshFormat(isObj: true),
        EMeshFormat.UEFormat => new UEFormatMeshFormat(),
        EMeshFormat.USD => new UsdMeshFormat(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported mesh format")
    };
}
