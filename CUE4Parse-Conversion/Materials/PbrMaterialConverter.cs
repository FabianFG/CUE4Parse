using System.Numerics;
using CUE4Parse_Conversion.Dto;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using static CUE4Parse_Conversion.Materials.MaterialParameters;

namespace CUE4Parse_Conversion.Materials;

/// <summary>Converts Unreal material parameters into PBR properties and texture references.</summary>
public sealed class PbrMaterialConverter(EMaterialDepth depth)
{
    public PbrMaterialDto Resolve(UMaterialInterface source)
    {
        var parameters = new MaterialParameters(source, depth);
        var (blendMode, cutoff, twoSided) = ResolveSurface(source);
        var baseColor = parameters.GetBaseColorTexture();
        var normal = parameters.GetNormalTexture();
        var (orm, ormLayout) = parameters.GetPackedTexture();
        var occlusion = parameters.GetOcclusionTexture();
        var (emission, emissive) = ResolveEmissive(parameters);

        return new PbrMaterialDto
        {
            BaseColor = ResolveBaseColor(parameters, blendMode),
            Metallic = parameters.GetMetallic(orm != null),
            Roughness = parameters.GetRoughness(orm != null),
            Emissive = emission,
            BlendMode = blendMode,
            AlphaCutoff = cutoff,
            DoubleSided = twoSided,
            BaseColorTexture = baseColor,
            NormalTexture = normal,
            OcclusionTexture = occlusion,
            OrmTexture = orm,
            OrmTextureLayout = ormLayout,
            EmissiveTexture = emissive,
            OpacityTexture = blendMode is EBlendMode.BLEND_Opaque ? null : ResolveOpacity(source)
        };
    }

    private static Vector4 ResolveBaseColor(MaterialParameters parameters, EBlendMode blendMode)
    {
        var tint = Vector4.One;
        if (parameters.GetBaseColorTint() is { } color)
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

    private static (EBlendMode BlendMode, float Cutoff, bool TwoSided) ResolveSurface(UMaterialInterface material)
    {
        var blendMode = EBlendMode.BLEND_Opaque;
        var cutoff = 0.333f;
        var twoSided = false;
        foreach (var current in GetInheritanceChain(material))
        {
            if (current is UMaterial parent)
            {
                blendMode = parent.BlendMode;
                cutoff = parent.OpacityMaskClipValue;
                twoSided = parent.TwoSided;
            }

            if (current is not UMaterialInstance { BasePropertyOverrides: { } overrides })
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

    private static Stack<UMaterialInterface> GetInheritanceChain(UMaterialInterface material)
    {
        var visited = new HashSet<UMaterialInterface>();
        var chain = new Stack<UMaterialInterface>();
        while (visited.Add(material))
        {
            chain.Push(material);
            if (material is not UMaterialInstance instance || instance.Parent is not { } parentReference || !parentReference.TryLoad<UMaterialInterface>(out var parent))
                break;

            material = parent;
        }

        return chain;
    }
}
