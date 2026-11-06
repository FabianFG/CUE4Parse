using System.Numerics;
using CUE4Parse_Conversion.Dto;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Options;
using static CUE4Parse_Conversion.Materials.MaterialParameters;

namespace CUE4Parse_Conversion.Materials;

/// <summary>Builds a PBR material description from parsed Unreal material parameters.</summary>
internal static class PbrMaterialConverter
{
    public static PbrMaterialDto Convert(MaterialParameters parameters)
    {
        var (blendMode, cutoff, twoSided) = parameters.GetSurface();
        var (orm, ormLayout) = parameters.GetPackedTexture();
        var (emission, emissive) = ResolveEmissive(parameters);

        Lazy<PbrMaterialDto?>? parent = null;
        if (parameters.Depth is not EMaterialDepth.TopLayerOnly)
        {
            parent = new Lazy<PbrMaterialDto?>(() => parameters.Parent?.Pbr);
        }

        return new PbrMaterialDto(parameters.Depth, parent)
        {
            BaseColor = ResolveBaseColor(parameters, blendMode),
            Metallic = parameters.GetMetallic(orm != null),
            Roughness = parameters.GetRoughness(orm != null),
            Emissive = emission,
            BlendMode = blendMode,
            AlphaCutoff = cutoff,
            DoubleSided = twoSided,
            BaseColorTexture = parameters.GetBaseColorTexture(),
            NormalTexture = parameters.GetNormalTexture(),
            OcclusionTexture = parameters.GetOcclusionTexture(),
            OrmTexture = orm,
            OrmTextureLayout = ormLayout,
            EmissiveTexture = emissive,
            OpacityTexture = blendMode is EBlendMode.BLEND_Opaque ? null : ResolveOpacity(parameters.Source)
        };
    }

    private static Vector4 ResolveBaseColor(MaterialParameters parameters, EBlendMode blendMode)
    {
        var tint = Vector4.One;
        var rules = new MeshExportRules(parameters.Source.Game);
        if (rules.UseDiffuseColor && parameters.GetBaseColorTint() is { } color)
        {
            tint = new Vector4(Clamp(color.R), Clamp(color.G), Clamp(color.B), 1);
        }

        tint.W = parameters.GetOpacity(blendMode);

        return tint;
    }

    private static (Vector3 Color, UTexture2D? Texture) ResolveEmissive(MaterialParameters parameters)
    {
        var emissionColor = parameters.GetEmissiveColor();
        var emission = Vector3.One;
        if (emissionColor is { } emissiveTint)
        {
            emission = new Vector3(Math.Max(0, emissiveTint.R), Math.Max(0, emissiveTint.G), Math.Max(0, emissiveTint.B));
        }

        // Skip the emissive texture when its color multiplier is zero
        if (emission == Vector3.Zero)
            return (Vector3.Zero, null);

        var emissive = parameters.GetEmissiveTexture();
        if (!emissionColor.HasValue && emissive == null)
        {
            emission = Vector3.Zero;
        }

        return (emission, emissive);
    }

    private static UTexture2D? ResolveOpacity(UMaterialInterface material)
    {
        var parameters = new CMaterialParams();
        material.GetParams(parameters);
        return parameters.Opacity?.TryLoad<UTexture2D>(out var mask) == true ? mask : null;
    }
}
