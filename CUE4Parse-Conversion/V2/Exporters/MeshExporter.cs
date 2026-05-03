using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse_Conversion.V2.Formats.Meshes;
using CUE4Parse_Conversion.V2.Options;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;

namespace CUE4Parse_Conversion.V2.Exporters;

public abstract class MeshExporter<T>(T mesh) : ExporterBase(mesh) where T : UObject
{
    protected abstract IReadOnlyList<ExportFile> BuildFiles(T original, IMeshExportFormat format);

    protected override IReadOnlyList<ExportFile> BuildExportFiles()
    {
        Log.Debug("Converting mesh to {Format} at {Quality} quality ({NaniteFormat})", Session.Options.MeshFormat, Session.Options.MeshQuality, Session.Options.NaniteMeshFormat);

        return BuildFiles(mesh, GetMeshFormat(Session.Options.MeshFormat));
    }

    protected Dictionary<string, string>? EnqueueMaterials(params MeshMaterial[] materials)
    {
        if (!Session.Options.ExportMaterials) return null;

        // TODO: currently only usda needs such thing, but maybe other formats will need it in the future, so we can keep it for now
        var paths = Session.Options.MeshFormat == EMeshFormat.USD
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : null;

        foreach (var slot in materials)
        {
            if (slot.Material?.TryLoad<UMaterialInterface>(out var material) == true)
            {
                Session.Add(material);
                if (paths != null)
                {
                    paths[slot.SlotName] = Resolve(material, "usda");
                }
            }
        }

        return paths;
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
