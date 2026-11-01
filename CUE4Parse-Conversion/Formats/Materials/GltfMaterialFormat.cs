using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Textures;
using SharpGLTF.Materials;

namespace CUE4Parse_Conversion.Formats.Materials;

/// <summary>Applies PBR material properties and textures to a glTF material.</summary>
internal sealed class GltfMaterialFormat(ExportOptions options, ExportSession session)
{
    // These images belong to the current mesh, not the session cache
    private readonly Dictionary<FMaterialTexture, ImageBuilder?> _images = [];

    public void Apply(PbrMaterialDto source, MaterialBuilder material)
    {
        material.WithBaseColor(source.BaseColor).WithDoubleSide(source.DoubleSided);
        if (source.BlendMode is EBlendMode.BLEND_Masked)
        {
            material.WithAlpha(source.UseOpacity ? AlphaMode.MASK : AlphaMode.OPAQUE, source.AlphaCutoff);
        }
        else if (source.UseOpacity)
        {
            material.WithAlpha(AlphaMode.BLEND);
        }

        AttachTexture(material, KnownChannel.BaseColor, source.BaseColorTexture, source.OpacityTexture, alpha: material.AlphaMode is AlphaMode.OPAQUE ? EMaterialTextureAlpha.Ignore : EMaterialTextureAlpha.Preserve);
        AttachTexture(material, KnownChannel.Normal, source.NormalTexture);

        var orm = AttachTexture(material, KnownChannel.MetallicRoughness, source.OrmTexture, layout: source.OrmTextureLayout);
        if (source.OcclusionTexture != null)
        {
            AttachTexture(material, KnownChannel.Occlusion, source.OcclusionTexture);
        }
        else if (orm != null && source.OrmTextureLayout is not (EMaterialTextureLayout.RSM or EMaterialTextureLayout.SRM or EMaterialTextureLayout.HRM))
        {
            material.WithChannelImage(KnownChannel.Occlusion, orm);
        }

        material.WithMetallicRoughness(source.GetMetallic(orm != null), source.Roughness);

        var hasEmission = AttachTexture(material, KnownChannel.Emissive, source.EmissiveTexture);
        if (hasEmission != null || source.Emissive != Vector3.Zero)
        {
            var emission = source.Emissive;
            var strength = Math.Max(1, Math.Max(emission.X, Math.Max(emission.Y, emission.Z)));
            material.WithEmissive(emission / strength, strength);
        }
    }

    private ImageBuilder? AttachTexture(MaterialBuilder material, KnownChannel channel, UTexture? texture, UTexture? opacity = null,
        EMaterialTextureLayout layout = EMaterialTextureLayout.ORM, EMaterialTextureAlpha alpha = EMaterialTextureAlpha.Ignore)
    {
        if (texture == null)
            return null;

        try
        {
            var image = GetTexture(new FMaterialTexture(texture, channel is KnownChannel.Normal, opacity, layout, alpha));
            if (image == null)
                return null;

            material.WithChannelImage(channel, image);
            return image;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Could not embed glTF {Channel} texture for {Material}", channel, material.Name);
            return null;
        }
    }

    private ImageBuilder? GetTexture(FMaterialTexture source)
    {
        if (_images.TryGetValue(source, out var cached))
            return cached;

        _images[source] = null; // Cache decode failures too

        var path = GetReusableTexturePath(source.Texture, allowJpeg: !source.NeedsConversion && source.Alpha is not EMaterialTextureAlpha.Preserve);
        var opacityPath = source.Opacity != null ? GetReusableTexturePath(source.Opacity) : null;

        var data = !source.NeedsConversion && path != null && File.Exists(path)
            ? File.ReadAllBytes(path)
            : MaterialTextureConverter.Encode(source, options, path, opacityPath);

        if (data == null)
            return null;

        ImageBuilder image = data;
        image.Name = source.Texture.Name;
        _images[source] = image;
        return image;
    }

    // Core glTF requires PNG/JPG, regardless of the texture export format chosen
    private string? GetReusableTexturePath(UTexture texture, bool allowJpeg = false)
    {
        if (!session.AttachMaterials)
            return null;

        var path = session.Cache.GetTexturePath(texture);
        if (path == null)
            return null;

        var extension = Path.GetExtension(path);
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            return path;
        if (allowJpeg && (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)))
            return path;

        return null;
    }
}
