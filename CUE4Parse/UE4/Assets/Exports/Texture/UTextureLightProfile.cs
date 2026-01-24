using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class UTextureLightProfile : UTexture2D
{
    public float Brightness { get; private set; }
    public float TextureMultiplier { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Brightness = GetOrDefault(nameof(Brightness), -1.0f);
        TextureMultiplier = GetOrDefault(nameof(TextureMultiplier), 1.0f);
    }
}
