using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class UVolumeTexture : UTexture
{
    public TextureAddress AddressMode { get; private set; }
    public override TextureAddress GetTextureAddressX() => AddressMode;
    public override TextureAddress GetTextureAddressY() => AddressMode;
    public override TextureAddress GetTextureAddressZ() => AddressMode;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        AddressMode = GetOrDefault<TextureAddress>(nameof(AddressMode));

        var stripFlags = new FStripDataFlags(Ar);
        var bCooked = Ar.ReadBoolean();

        if (bCooked)
        {
            DeserializeCookedPlatformData(Ar);
        }
    }
}
