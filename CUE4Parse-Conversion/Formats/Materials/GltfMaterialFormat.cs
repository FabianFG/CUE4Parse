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
    private readonly Dictionary<(UTexture Texture, bool Normal, UTexture? Opacity, EMaterialTextureLayout Layout, bool PreserveAlpha), ImageBuilder?> _images = [];

    public void Apply(PbrMaterialDto source, MaterialBuilder material)
    {
        material.WithBaseColor(source.BaseColor).WithDoubleSide(source.DoubleSided);
        if (source.BlendMode is EBlendMode.BLEND_Masked)
        {
            material.WithAlpha(AlphaMode.MASK, source.AlphaCutoff);
        }
        else if (source.BlendMode is EBlendMode.BLEND_Translucent or EBlendMode.BLEND_Additive or EBlendMode.BLEND_AlphaComposite)
        {
            material.WithAlpha(AlphaMode.BLEND);
        }

        AttachTexture(material, KnownChannel.BaseColor, source.BaseColorTexture, source.OpacityTexture, preserveAlpha: source.BlendMode is not EBlendMode.BLEND_Opaque);
        AttachTexture(material, KnownChannel.Normal, source.NormalTexture);

        var orm = AttachTexture(material, KnownChannel.MetallicRoughness, source.OrmTexture, layout: source.OrmTextureLayout);
        if (source.OcclusionTexture != null)
        {
            AttachTexture(material, KnownChannel.Occlusion, source.OcclusionTexture);
        }
        else if (orm != null && source.OrmTextureLayout is not (EMaterialTextureLayout.RSM or EMaterialTextureLayout.SRM))
        {
            material.WithChannelImage(KnownChannel.Occlusion, orm);
        }

        // A failed image decode should leave a neutral surface rather than glTF's metallic default
        material.WithMetallicRoughness(source.OrmTexture != null && orm == null ? 0 : source.Metallic, source.Roughness);

        var hasEmission = AttachTexture(material, KnownChannel.Emissive, source.EmissiveTexture);
        if (hasEmission != null || source.Emissive != Vector3.Zero)
        {
            var emission = source.Emissive;
            var strength = Math.Max(1, Math.Max(emission.X, Math.Max(emission.Y, emission.Z)));
            material.WithEmissive(emission / strength, strength);
        }
    }

    private ImageBuilder? AttachTexture(MaterialBuilder material, KnownChannel channel, UTexture? texture, UTexture? opacity = null,
        EMaterialTextureLayout layout = EMaterialTextureLayout.ORM, bool preserveAlpha = false)
    {
        if (texture == null)
            return null;

        try
        {
            var image = GetTexture(texture, channel is KnownChannel.Normal, opacity, layout, preserveAlpha);
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

    private ImageBuilder? GetTexture(UTexture texture, bool normal, UTexture? opacity, EMaterialTextureLayout layout, bool preserveAlpha)
    {
        var key = (texture, normal, opacity, layout, preserveAlpha);
        if (_images.TryGetValue(key, out var cached))
            return cached;

        _images[key] = null; // Cache decode failures too

        var convert = normal || opacity != null || layout is not EMaterialTextureLayout.ORM;
        var path = GetReusableTexturePath(texture, allowJpeg: !convert && !preserveAlpha);
        var opacityPath = opacity != null ? GetReusableTexturePath(opacity) : null;

        byte[]? data;
        if (path != null && !convert)
        {
            data = File.ReadAllBytes(path);
        }
        else
        {
            data = MaterialTextureConverter.Encode(texture, options, normal, opacity, layout, path, opacityPath);
        }

        if (data == null)
            return null;

        ImageBuilder image = data;
        image.Name = texture.Name;
        _images[key] = image;
        return image;
    }

    // Core glTF requires PNG/JPG, regardless of the texture export format chosen
    private string? GetReusableTexturePath(UTexture texture, bool allowJpeg = false)
    {
        if (!session.EmbedMaterials)
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
