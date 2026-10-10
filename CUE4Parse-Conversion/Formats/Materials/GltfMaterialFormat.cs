using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Textures;
using SharpGLTF.Materials;

namespace CUE4Parse_Conversion.Formats.Materials;

/// <summary>Applies PBR material properties and textures to a glTF material.</summary>
internal sealed class GltfMaterialFormat(ExportOptions options, TextureExportCache textures)
{
    private readonly MaterialTextureConverter _converter = new(options, textures);
    private readonly Dictionary<(UTexture Texture, bool Normal, UTexture? Opacity, EMaterialTextureLayout Layout), ImageBuilder?> _images = [];

    public void Apply(PbrMaterialDto source, MaterialBuilder material)
    {
        material.WithBaseColor(source.BaseColor).WithDoubleSide(source.DoubleSided);
        if (source.BlendMode == EBlendMode.BLEND_Masked)
        {
            material.WithAlpha(AlphaMode.MASK, source.AlphaCutoff);
        }
        else if (source.BlendMode is EBlendMode.BLEND_Translucent or EBlendMode.BLEND_Additive or EBlendMode.BLEND_AlphaComposite)
        {
            material.WithAlpha(AlphaMode.BLEND);
        }

        AttachTexture(material, KnownChannel.BaseColor, source.BaseColorTexture, source.OpacityTexture);
        AttachTexture(material, KnownChannel.Normal, source.NormalTexture);

        var hasOrm = AttachTexture(material, KnownChannel.MetallicRoughness, source.OrmTexture, layout: source.OrmTextureLayout);
        if (source.OcclusionTexture != null)
        {
            AttachTexture(material, KnownChannel.Occlusion, source.OcclusionTexture);
        }
        else if (hasOrm && source.OrmTextureLayout is not (EMaterialTextureLayout.RSM or EMaterialTextureLayout.SRM))
        {
            AttachTexture(material, KnownChannel.Occlusion, source.OrmTexture, layout: source.OrmTextureLayout);
        }

        // A failed image decode should leave a neutral surface rather than glTF's metallic default
        material.WithMetallicRoughness(source.OrmTexture != null && !hasOrm ? 0 : source.Metallic, source.Roughness);

        var hasEmission = AttachTexture(material, KnownChannel.Emissive, source.EmissiveTexture);
        if (hasEmission || source.Emissive != Vector3.Zero)
        {
            var emission = source.Emissive;
            var strength = Math.Max(1, Math.Max(emission.X, Math.Max(emission.Y, emission.Z)));
            material.WithEmissive(emission / strength, strength);
        }
    }

    private bool AttachTexture(MaterialBuilder material, KnownChannel channel, UTexture? texture, UTexture? opacity = null, EMaterialTextureLayout layout = EMaterialTextureLayout.ORM)
    {
        if (texture == null)
            return false;

        try
        {
            var image = GetTexture(texture, channel == KnownChannel.Normal, opacity, layout);
            if (image == null)
                return false;

            material.WithChannelImage(channel, image);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Could not embed glTF {Channel} texture for {Material}", channel, material.Name);
            return false;
        }
    }

    private ImageBuilder? GetTexture(UTexture texture, bool normal, UTexture? opacity, EMaterialTextureLayout layout)
    {
        var key = (texture, normal, opacity, layout);
        if (_images.TryGetValue(key, out var cached))
            return cached;

        _images[key] = null; // Cache decode failures too
        var decoded = _converter.GetTexture(texture, isNormalMap: normal, opacity: opacity, layout: layout);
        if (decoded == null)
            return null;

        // Core glTF requires PNG/JPG, regardless of the texture export format chosen
        ImageBuilder image = textures.Encode(decoded, ETextureFormat.Png, false, pngCompressionLevel: options.PngCompressionLevel).Data;
        image.Name = texture.Name;
        _images[key] = image;
        return image;
    }
}
