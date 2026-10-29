using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Dto;

namespace CUE4Parse_Conversion.Materials;

/// <summary>Parses material inheritance and resolves PBR values from known parameter aliases.</summary>
internal sealed partial class MaterialParameters
{
    private static readonly string[] _baseColorTextures = [..CMaterialParams2.Diffuse[0], "Base", CMaterialParams2.FallbackDiffuse];
    private static readonly string[] _normalTextures = [..CMaterialParams2.Normals[0], CMaterialParams2.FallbackNormals];
    private static readonly string[] _emissiveTextures = CMaterialParams2.Emissive[0];
    private static readonly string[] _occlusionTextures = ["AO", "AmbientOcclusion", "AmbientOcclusionTexture", "Occlusion", "OcclusionTexture"];

    // Generic "Color" parameters do not reliably identify an emissive multiplier!
    private static readonly string[] _emissiveColors = [..CMaterialParams2.EmissiveColors[0].Where(name => name != "Color")];

    private static readonly string[] _metallicMultipliers = ["Main_MetalValue", "MetallicScale", "Metallic Multiply", "Metallic Amount"];
    private static readonly string[] _roughnessMultipliers = ["Main_RoughValue", "RoughnessScale", "Roughness Multiply", "Roughness Amount"];
    private static readonly string[] _metallicConstants = ["MetallicValue", "Metallic", "Main_MetalValue"];
    private static readonly string[] _roughnessConstants = ["RoughnessValue", "RoughnessMax", "SpecRoughnessMax", "Roughness", "Main_RoughValue", "CorneaRoughness"];
    private static readonly string[] _opacity = ["Opacity"];
    private static readonly string[] _opacityMask = ["OpacityMask"];

    private static readonly HashSet<string> _secondaryPackedTextures = new(CMaterialParams2.SpecularMasks.Skip(1).SelectMany(names => names), StringComparer.OrdinalIgnoreCase);

    private static readonly EMaterialTextureLayout[] _textureLayouts = Enum.GetValues<EMaterialTextureLayout>();

    private static readonly (string Name, EMaterialTextureLayout Layout)[] _packedTextureNames = [.. CMaterialParams2.SpecularMasks[0]
        .Select(name => (Name: name, Layout: GetPackedTextureLayout(name)))
        .Where(alias => alias.Layout.HasValue)
        .OrderBy(alias => alias.Layout)
        .Select(alias => (alias.Name, alias.Layout!.Value))];

    private readonly Lazy<CMaterialParams2> _local;
    private readonly Lazy<MaterialParameters?> _parent;
    private readonly Lazy<MaterialParameters[]> _inheritance;
    private readonly Lazy<CMaterialParams2[]> _layers;
    private readonly Lazy<HashSet<string>?> _activeTextureParameters;
    private readonly Lazy<CMaterialParams2> _combined;
    private readonly Lazy<PbrMaterialDto> _pbr;

    public UMaterialInterface Source { get; }
    public EMaterialDepth Depth { get; }
    public MaterialParameters? Parent => Depth is EMaterialDepth.TopLayerOnly ? null : _parent.Value;
    public PbrMaterialDto Pbr => _pbr.Value;
    public CMaterialParams2 Parameters => _combined.Value;

    public MaterialParameters(UMaterialInterface material, EMaterialDepth depth, Func<UMaterialInterface, EMaterialDepth, MaterialParameters> resolve)
    {
        Source = material;
        Depth = depth;
        _local = new Lazy<CMaterialParams2>(() => ReadParameters(material, material is UMaterialInstance ? EMaterialDepth.TopLayerOnly : depth));
        _parent = new Lazy<MaterialParameters?>(() =>
        {
            if (material is not UMaterialInstance instance || instance.Parent?.TryLoad<UMaterialInterface>(out var parent) != true || parent == null)
                return null;

            return resolve(parent, depth);
        });
        _inheritance = new Lazy<MaterialParameters[]>(ReadInheritance);
        _layers = new Lazy<CMaterialParams2[]>(() => depth is EMaterialDepth.TopLayerOnly
            ? [_local.Value]
            : [.. _inheritance.Value.Select(layer => layer._local.Value)]);
        _activeTextureParameters = new Lazy<HashSet<string>?>(() => GetActiveTextureParameters(material, depth));
        _combined = depth is EMaterialDepth.TopLayerOnly ? _local : new Lazy<CMaterialParams2>(CombineParameters);
        _pbr = new Lazy<PbrMaterialDto>(() => PbrMaterialConverter.Convert(this));
    }

    private CMaterialParams2[] Layers => _layers.Value;

    private MaterialParameters[] ReadInheritance()
    {
        var visited = new HashSet<MaterialParameters>();
        var layers = new List<MaterialParameters>();
        for (MaterialParameters? current = this; current != null; current = current._parent.Value)
        {
            if (!visited.Add(current))
                throw new InvalidDataException($"Material inheritance contains a cycle at {current.Source.GetPathName()}.");

            layers.Add(current);
        }

        return [.. layers];
    }

    private CMaterialParams2 CombineParameters()
    {
        var result = new CMaterialParams2();
        foreach (var layer in _inheritance.Value.Reverse())
        {
            var local = layer._local.Value;
            Copy(local, result);
            if (layer.Source is not UMaterialInstance || layer.Source is UMaterialInstance { BasePropertyOverrides: not null })
            {
                result.BlendMode = local.BlendMode;
                result.ShadingModel = local.ShadingModel;
            }
        }

        return result;

        static void Copy(CMaterialParams2 source, CMaterialParams2 target)
        {
            foreach (var (name, value) in source.Textures)
                target.Textures[name] = value;
            foreach (var (name, value) in source.Colors)
                target.Colors[name] = value;
            foreach (var (name, value) in source.Scalars)
                target.Scalars[name] = value;
            foreach (var (name, value) in source.Switches)
                target.Switches[name] = value;
            foreach (var (name, value) in source.Properties)
                target.Properties[name] = value;
        }
    }

    private static HashSet<string>? GetActiveTextureParameters(UMaterialInterface material, EMaterialDepth depth)
    {
        var current = material;
        var visited = new HashSet<UMaterialInterface>();
        while (visited.Add(current))
        {
            var uniforms = current.LoadedMaterialResources?.Select(resource => resource.LoadedShaderMap?.Content)
                .OfType<FMaterialShaderMapContent>().Select(content => content.MaterialCompilationOutput?.UniformExpressionSet)
                .OfType<FUniformExpressionSet>().ToArray();
            if (uniforms?.Length > 0)
            {
                var bindings = uniforms.SelectMany(uniform => uniform.UniformTextureParameters).SelectMany(parameters => parameters).ToArray();
                if (bindings.Any(binding => binding.ParameterInfo is not { Association: EMaterialParameterAssociation.GlobalParameter }))
                    return null;

                var names = bindings.Select(binding => binding.ParameterInfo!.Name).Where(name => !name.IsNone).Select(name => name.Text).ToHashSet(StringComparer.OrdinalIgnoreCase);
                return names.Count > 0 ? names : null;
            }

            if (depth is EMaterialDepth.TopLayerOnly || current is not UMaterialInstance { bHasStaticPermutationResource: false, Parent: { } parent } || !parent.TryLoad<UMaterialInterface>(out current))
                return null;
        }

        return null;
    }

    public (EBlendMode BlendMode, float Cutoff, bool TwoSided) GetSurface()
    {
        var blendMode = EBlendMode.BLEND_Opaque;
        var cutoff = 0.333f;
        var twoSided = false;

        foreach (var layer in _inheritance.Value.Reverse())
        {
            if (layer.Source is UMaterial material)
            {
                blendMode = material.BlendMode;
                cutoff = material.OpacityMaskClipValue;
                twoSided = material.TwoSided;
            }

            if (layer.Source is not UMaterialInstance { BasePropertyOverrides: { } overrides })
                continue;

            if (overrides.bOverride_BlendMode)
            {
                blendMode = overrides.BlendMode;
            }

            if (overrides.bOverride_OpacityMaskClipValue)
            {
                cutoff = overrides.OpacityMaskClipValue;
            }

            if (overrides.bOverride_TwoSided)
            {
                twoSided = overrides.TwoSided;
            }
        }

        return (blendMode, Clamp(cutoff), twoSided);
    }

    public UTexture2D? GetBaseColorTexture() => GetTexture(_baseColorTextures) ?? GetFallbackColorTexture();
    public UTexture2D? GetNormalTexture() => GetTexture(_normalTextures, matchSuffix: _activeTextureParameters.Value != null);
    public UTexture2D? GetEmissiveTexture() => GetTexture(_emissiveTextures);
    public UTexture2D? GetOcclusionTexture() => GetTexture(_occlusionTextures, matchSuffix: true);

    public FLinearColor? GetBaseColorTint() => GetColor(CMaterialParams2.DiffuseColors[0]);
    public FLinearColor? GetEmissiveColor() => GetColor(_emissiveColors);

    public float GetMetallic(bool hasPackedTexture)
    {
        if (hasPackedTexture)
            return GetScalar(_metallicMultipliers, 1);

        return GetScalar(_metallicConstants, 0);
    }

    public float GetRoughness(bool hasPackedTexture)
    {
        if (hasPackedTexture)
            return GetScalar(_roughnessMultipliers, 1);

        return GetScalar(_roughnessConstants, 1);
    }

    public float GetOpacity(EBlendMode blendMode)
    {
        if (blendMode == EBlendMode.BLEND_Masked)
            return GetScalar(_opacityMask, 1);
        if (blendMode == EBlendMode.BLEND_Opaque)
            return 1;

        return GetScalar(_opacity, 1);
    }

    public (UTexture2D? Texture, EMaterialTextureLayout Layout) GetPackedTexture()
    {
        foreach (var layer in Layers)
        {
            foreach (var (name, layout) in _packedTextureNames)
            {
                if (layer.Textures.TryGetValue(name, out var reference) && IsTextureActive(name, reference) && reference.TryLoad<UTexture2D>(out var texture))
                    return (texture, layout);
            }

            if (FindPackedTexture(layer, referencesOnly: false) is { } parameter)
                return parameter;
            if (FindPackedTexture(layer, referencesOnly: true) is { } referenced)
                return referenced;
        }

        return (null, EMaterialTextureLayout.ORM);
    }

    private UTexture2D? GetTexture(string[] names, bool matchSuffix = false)
    {
        foreach (var layer in Layers)
        {
            if (FindTexture(layer, names) is { } texture)
                return texture;
            if (matchSuffix && FindTextureBySuffix(layer, names) is { } named)
                return named;
        }

        return null;
    }

    private UTexture2D? GetFallbackColorTexture()
    {
        foreach (var layer in Layers)
        {
            foreach (var (name, reference) in layer.Textures)
            {
                if (!IsTextureActive(name, reference)) continue;
                if (reference.TryLoad<UTexture2D>(out var texture) && texture != null && IsBaseColorTexture(name, texture))
                    return texture;
            }
        }

        return null;
    }

    // Stolen from https://github.com/FModel/Snooper/blob/3ad5b935cd07cc4b0ba05f452a5b0f6bea0778b6/Snooper/Rendering/Cache/MaterialNode.cs#L316
    private static bool IsBaseColorTexture(string parameterName, UTexture texture)
    {
        if (texture.CompressionSettings is not (TextureCompressionSettings.TC_Default or TextureCompressionSettings.TC_BC7 or TextureCompressionSettings.TC_LQ) ||
            texture.LODGroup is TextureGroup.TEXTUREGROUP_WorldNormalMap or TextureGroup.TEXTUREGROUP_CharacterNormalMap or
                TextureGroup.TEXTUREGROUP_WeaponNormalMap or TextureGroup.TEXTUREGROUP_VehicleNormalMap or
                TextureGroup.TEXTUREGROUP_WorldSpecular or TextureGroup.TEXTUREGROUP_CharacterSpecular or
                TextureGroup.TEXTUREGROUP_WeaponSpecular or TextureGroup.TEXTUREGROUP_VehicleSpecular or
                TextureGroup.TEXTUREGROUP_UI or TextureGroup.TEXTUREGROUP_Lightmap or TextureGroup.TEXTUREGROUP_Shadowmap or
                TextureGroup.TEXTUREGROUP_Skybox or TextureGroup.TEXTUREGROUP_ImpostorNormalDepth || OcclusionName().IsMatch(parameterName))
            return false;

        if (DiffuseName().IsMatch(parameterName))
            return true;

        return texture.SRGB && !EmissiveName().IsMatch(parameterName);
    }

    private FLinearColor? GetColor(string[] names)
    {
        foreach (var layer in Layers)
        {
            if (layer.TryGetLinearColor(out var color, names))
                return color;
        }

        return null;
    }

    private float GetScalar(string[] names, float fallback)
    {
        foreach (var layer in Layers)
        {
            if (layer.TryGetScalar(out var value, names))
                return Clamp(value);
        }

        return fallback;
    }

    private static CMaterialParams2 ReadParameters(UMaterialInterface material, EMaterialDepth depth)
    {
        var parameters = new CMaterialParams2();
        material.GetParams(parameters, depth);
        return parameters;
    }

    private bool IsTextureActive(string name, ILoadableObject reference) =>
        _activeTextureParameters.Value is not { } active || active.Contains(name) || reference is FPackageIndex index && name == index.Name;

    private UTexture2D? FindTexture(CMaterialParams2 parameters, IEnumerable<string> names)
        => parameters.GetTextures(names.Where(name => parameters.Textures.TryGetValue(name, out var reference) && IsTextureActive(name, reference))).OfType<UTexture2D>().FirstOrDefault();

    private UTexture2D? FindTextureBySuffix(CMaterialParams2 parameters, string[] names)
    {
        foreach (var (name, reference) in parameters.Textures)
        {
            if (!IsTextureActive(name, reference)) continue;
            var matchesName = MatchesTextureSuffix(name, names);
            // A loaded reference may have a different name, don't load unrelated assets to check it
            if (!matchesName && (reference is not FPackageIndex index || name != index.Name || index.ResolvedObject?.Object is not { IsValueCreated: true }))
                continue;
            if (!reference.TryLoad<UTexture2D>(out var texture))
                continue;
            if (matchesName || MatchesTextureSuffix(texture.Name, names))
                return texture;
        }

        return null;
    }

    private (UTexture2D Texture, EMaterialTextureLayout Layout)? FindPackedTexture(CMaterialParams2 parameters, bool referencesOnly)
    {
        (UTexture2D Texture, EMaterialTextureLayout Layout)? result = null;
        foreach (var (name, reference) in parameters.Textures)
        {
            var isReference = reference is FPackageIndex index && name == index.Name;
            if (isReference != referencesOnly || !IsTextureActive(name, reference))
                continue;

            var layout = GetPackedTextureLayout(name);
            // Check unmatched reference names only when the texture is already loaded
            if (layout == null && (!referencesOnly || reference is not FPackageIndex { ResolvedObject.Object.IsValueCreated: true }))
                continue;
            if (layout.HasValue && result is { } best && (int) layout.Value >= (int) best.Layout)
                continue;
            if (!reference.TryLoad<UTexture2D>(out var texture))
                continue;

            layout ??= GetPackedTextureLayout(texture.Name);
            if (layout.HasValue && (result == null || (int) layout.Value < (int) result.Value.Layout))
                result = (texture, layout.Value);
        }

        return result;
    }

    private static EMaterialTextureLayout? GetPackedTextureLayout(string name)
    {
        if (_secondaryPackedTextures.Contains(name))
            return null;

        var layout = MatchPackedTextureLayout(name);
        if (layout.HasValue)
            return layout;

        var abbreviated = ChannelNames().Replace(name, static match => match.Value[..1]);
        if (abbreviated == name)
            return null;

        return MatchPackedTextureLayout(abbreviated);
    }

    private static bool MatchesTextureSuffix(string name, string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (!name.EndsWith(alias, StringComparison.OrdinalIgnoreCase))
                continue;

            var prefixLength = name.Length - alias.Length;
            if (prefixLength == 0 || !char.IsLetterOrDigit(name[prefixLength - 1]))
                return true;
        }

        return false;
    }

    private static EMaterialTextureLayout? MatchPackedTextureLayout(string name)
    {
        var matches = PackedTextureName().Matches(name);
        foreach (var layout in _textureLayouts)
        {
            var group = layout.ToString();
            foreach (Match match in matches)
            {
                if (match.Groups[group].Success)
                    return layout;
            }
        }

        return null;
    }

    [GeneratedRegex(CMaterialParams2.RegexDiffuse, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DiffuseName();

    [GeneratedRegex(CMaterialParams2.RegexEmissive, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmissiveName();

    [GeneratedRegex(CMaterialParams2.RegexOcclusion, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OcclusionName();

    [GeneratedRegex(CMaterialParams2.RegexChannelNames, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ChannelNames();

    [GeneratedRegex(CMaterialParams2.RegexPackedTextureLayout, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex PackedTextureName();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float Clamp(float value) => float.IsFinite(value) ? Math.Clamp(value, 0, 1) : 1;
}
