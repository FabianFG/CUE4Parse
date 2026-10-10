using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Dto;

namespace CUE4Parse_Conversion.Materials;

/// <summary>Resolves PBR values and textures from known aliases.</summary>
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

    private readonly CMaterialParams2[] _layers;

    public MaterialParameters(UMaterialInterface material, EMaterialDepth depth)
    {
        var local = ReadParameters(material, EMaterialDepth.TopLayerOnly);
        if (depth is EMaterialDepth.TopLayerOnly)
        {
            _layers = [local];
            return;
        }

        _layers = [local, ReadParameters(material, depth)];
    }

    public UTexture2D? GetBaseColorTexture() => GetTexture(_baseColorTextures) ?? GetFallbackColorTexture();
    public UTexture2D? GetNormalTexture() => GetTexture(_normalTextures);
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
        foreach (var layer in _layers)
        {
            foreach (var (name, layout) in _packedTextureNames)
            {
                if (layer.Textures.TryGetValue(name, out var reference) && reference.TryLoad<UTexture2D>(out var texture))
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
        foreach (var layer in _layers)
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
        foreach (var layer in _layers)
        {
            UTexture2D? fallback = null;
            foreach (var texture in layer.GetTextures(layer.Textures.Keys).OfType<UTexture2D>())
            {
                fallback ??= texture;
                // References include engine defaults and linear shader data as well as color textures
                // This should probably be done for 3D Viewer too, although it's far from perfect
                if (texture.SRGB && !texture.IsNormalMap && !texture.GetPathName().StartsWith("/Engine/", StringComparison.OrdinalIgnoreCase))
                    return texture;
            }
            if (fallback != null)
                return fallback;
        }

        return null;
    }

    private FLinearColor? GetColor(string[] names)
    {
        foreach (var layer in _layers)
        {
            if (layer.TryGetLinearColor(out var color, names))
                return color;
        }

        return null;
    }

    private float GetScalar(string[] names, float fallback)
    {
        foreach (var layer in _layers)
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

    private static UTexture2D? FindTexture(CMaterialParams2 parameters, IEnumerable<string> names)
        => parameters.GetTextures(names).OfType<UTexture2D>().FirstOrDefault();

    private static UTexture2D? FindTextureBySuffix(CMaterialParams2 parameters, string[] names)
    {
        foreach (var (name, reference) in parameters.Textures)
        {
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

    private static (UTexture2D Texture, EMaterialTextureLayout Layout)? FindPackedTexture(CMaterialParams2 parameters, bool referencesOnly)
    {
        (UTexture2D Texture, EMaterialTextureLayout Layout)? result = null;
        foreach (var (name, reference) in parameters.Textures)
        {
            var isReference = reference is FPackageIndex index && name == index.Name;
            if (isReference != referencesOnly)
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

    // This needs to be tested on more games
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

    [GeneratedRegex(@"Ambient[ _]*Occlusion|Occlusion|Occl|Occ|Roughness|Rough|Metallic|Metal|Met|Specular|Spec", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ChannelNames();

    [GeneratedRegex("""
        (?: ^ | [^a-z0-9] ) (?: Tex(?:ture)? [ _]* )?
        (?:
            (?<ORM> O[ _/]*R[ _/]*ME? | AO?[ _/]*R[ _/]*M )
          | (?<RMA> R[ _/]*M[ _/]*(?: AO? | O ) )
          | (?<ROM> R[ _/]*(?: O | AO? )[ _/]*M )
          | (?<RSM> R[ _/]*S[ _/]*M )
          | (?<MRA> M[ _/]*R[ _/]*A[OES]? )
          | (?<MRO> M[ _/]*R[ _/]*O(?: A | S | Dp )? )
          | (?<SRM> S[ _/]*R[ _/]*M )
        )
        (?= $ | [^a-z0-9] | (?: Map | Tex(?:ture)? | VT )(?: $ | [^a-z0-9] ) )
        """, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex PackedTextureName();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float Clamp(float value) => float.IsFinite(value) ? Math.Clamp(value, 0, 1) : 1;
}
