using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace CUE4Parse_Conversion.Dto;

public sealed class PbrMaterialDto
{
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
    SRM
}
