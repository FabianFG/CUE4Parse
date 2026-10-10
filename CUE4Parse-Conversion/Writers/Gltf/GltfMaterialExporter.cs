using System.Numerics;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Materials;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Material;
using SharpGLTF.Materials;

namespace CUE4Parse_Conversion.Writers.Gltf;

/// <summary>Loads mesh materials and reuses their glTF builders across sections and LODs.</summary>
internal sealed class GltfMaterialExporter(ExportOptions options, TextureExportCache textures)
{
    private readonly PbrMaterialConverter _converter = new(options.MaterialDepth);
    private readonly GltfMaterialFormat _format = new(options, textures);
    private readonly Dictionary<UMaterialInterface, MaterialBuilder> _materials = [];

    public MaterialBuilder Build(MeshMaterialDto? slot, string fallbackName)
    {
        var name = slot?.SlotName ?? fallbackName;
        MaterialBuilder? result = null;
        try
        {
            if (options.ExportMaterials && slot?.Material is { } reference && reference.TryLoad<UMaterialInterface>(out var material))
            {
                if (_materials.TryGetValue(material, out var cached))
                    return cached;

                result = CreateMaterial(name);
                _format.Apply(_converter.Resolve(material), result);
                _materials[material] = result;
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Could not convert glTF material {Material}", name);
        }

        return result ?? CreateMaterial(name);
    }

    private static MaterialBuilder CreateMaterial(string name)
        => new MaterialBuilder(name).WithBaseColor(Vector4.One).WithMetallicRoughness(0, 1);
}
