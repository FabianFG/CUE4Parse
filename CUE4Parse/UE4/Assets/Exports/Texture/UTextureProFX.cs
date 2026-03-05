using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

// RawData is the data of a ".map" format
public class UTextureProFXParent : UTexture
{
    public int SizeX;
    public int SizeY;
    public EPixelFormat Format;
    public FByteBulkData RawData;
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        SizeX = Ar.Read<int>();
        SizeY = Ar.Read<int>();
        Format = (EPixelFormat)Ar.Read<int>();
        RawData = new FByteBulkData(Ar);
    }
}

public class UTextureProFXChild : UTexture
{
    public int SizeX;
    public int SizeY;
    public EPixelFormat Format;
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        SizeX = Ar.Read<int>();
        SizeY = Ar.Read<int>();
        Format = (EPixelFormat)Ar.Read<int>();
    }
}
