using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class UTexture2DArray : UTexture
{
    public TextureAddress AddressX { get; private set; }
    public TextureAddress AddressY { get; private set; }
    public TextureAddress AddressZ { get; private set; }

    public override TextureAddress GetTextureAddressX() => AddressX;
    public override TextureAddress GetTextureAddressY() => AddressY;
    public override TextureAddress GetTextureAddressZ() => AddressZ;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        AddressX = GetOrDefault<TextureAddress>(nameof(AddressX));
        AddressY = GetOrDefault<TextureAddress>(nameof(AddressY));
        AddressZ = GetOrDefault<TextureAddress>(nameof(AddressZ));

        var stripFlags = new FStripDataFlags(Ar);
        var bCooked = Ar.ReadBoolean();

        if (bCooked)
        {
            DeserializeCookedPlatformData(Ar);
        }
    }
}
