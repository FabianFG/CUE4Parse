using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace CUE4Parse_Conversion.Dto;

public sealed class PbrMaterialDto
{
    private readonly Lazy<PbrMaterialDto?>? _parent;

    public EMaterialDepth Depth { get; }
    public PbrMaterialDto? Parent => _parent?.Value;

    internal PbrMaterialDto(EMaterialDepth depth, Lazy<PbrMaterialDto?>? parent)
    {
        Depth = depth;
        _parent = parent;
    }

    public Vector4 BaseColor { get; init; } = Vector4.One;
    public float Metallic { get; init; }
    public float Roughness { get; init; } = 1;
    public Vector3 Emissive { get; init; }
    public EBlendMode BlendMode { get; init; }
    public float AlphaCutoff { get; init; } = 0.333f;
    public bool DoubleSided { get; init; }
    public UTexture? BaseColorTexture { get; init; }
    public UTexture? NormalTexture { get; init; }
    public UTexture? OcclusionTexture { get; init; }
    public UTexture? OrmTexture { get; init; }
    public EMaterialTextureLayout OrmTextureLayout { get; init; }
    public UTexture? EmissiveTexture { get; init; }
    public UTexture? OpacityTexture { get; init; }

    // This is just a guess to prevent making whole mesh invisible
    // should be replaced with something better but for now I have no idea
    public bool UseOpacity => BlendMode is EBlendMode.BLEND_Masked
        ? DoubleSided || OpacityTexture != null || BaseColor.W < 1
        : BlendMode is EBlendMode.BLEND_Translucent or EBlendMode.BLEND_Additive or EBlendMode.BLEND_AlphaComposite;

    // Failed texture export should leave a neutral surface
    public float GetMetallic(bool hasOrmTexture) => OrmTexture != null && !hasOrmTexture ? 0 : Metallic;
}

/// <summary>Channel order of a packed material texture.</summary>
public enum EMaterialTextureLayout
{
    /// <summary>Occlusion (R), Roughness (G), Metallic (B).</summary>
    ORM,
    /// <summary>Roughness (R), Metallic (G), Ambient Occlusion (B).</summary>
    RMA,
    /// <summary>Roughness (R), Occlusion (G), Metallic (B).</summary>
    ROM,
    /// <summary>Roughness (R), Specular (G), Metallic (B); Occlusion is stored separately.</summary>
    RSM,
    /// <summary>Metallic (R), Roughness (G), Ambient Occlusion (B).</summary>
    MRA,
    /// <summary>Metallic (R), Roughness (G), Occlusion (B).</summary>
    MRO,
    /// <summary>Specular (R), Roughness (G), Metallic (B); Occlusion is stored separately.</summary>
    SRM,
    /// <summary>Height (R), Roughness (G), Metallic (B); Occlusion is stored separately.</summary>
    HRM
}

internal static class MaterialTextureLayoutExtensions
{
    internal static (int Occlusion, int Roughness, int Metallic) GetPackedChannels(this EMaterialTextureLayout layout) => layout switch
    {
        EMaterialTextureLayout.ORM => (0, 1, 2),
        EMaterialTextureLayout.RMA => (2, 0, 1),
        EMaterialTextureLayout.ROM => (1, 0, 2),
        EMaterialTextureLayout.RSM => (-1, 0, 2),
        EMaterialTextureLayout.MRA or EMaterialTextureLayout.MRO => (2, 1, 0),
        EMaterialTextureLayout.SRM or EMaterialTextureLayout.HRM => (-1, 1, 2),
        _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null)
    };
}
