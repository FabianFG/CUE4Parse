using System.Numerics;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Materials;
using CUE4Parse_Conversion.Options;
using CUE4Parse.UE4.Assets.Exports.Material;
using SharpGLTF.Materials;

namespace CUE4Parse_Conversion.Writers.Gltf;

/// <summary>Builds glTF materials from cached PBR descriptions.</summary>
internal sealed class GltfMaterialExporter(ExportOptions options, ExportSession session)
{
    private readonly GltfMaterialFormat _format = new(options, session);
    private readonly Dictionary<UMaterialInterface, MaterialBuilder> _materials = [];

    public MaterialBuilder Build(MeshMaterialDto? slot, string fallbackName)
    {
        var name = slot?.SlotName ?? fallbackName;
        try
        {
            if (options.ExportMaterials && slot?.Material is { } reference && reference.TryLoad<UMaterialInterface>(out var material))
            {
                if (_materials.TryGetValue(material, out var cached))
                    return cached;

                var result = CreateMaterial(name);
                _format.Apply(session.ResolveMaterial(material, options.MaterialDepth), result);
                _materials[material] = result;
                return result;
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Could not convert glTF material {Material}", name);
        }

        return CreateMaterial(name);
    }

    private static MaterialBuilder CreateMaterial(string name)
        => new MaterialBuilder(name).WithBaseColor(Vector4.One).WithMetallicRoughness(0, 1);
}
