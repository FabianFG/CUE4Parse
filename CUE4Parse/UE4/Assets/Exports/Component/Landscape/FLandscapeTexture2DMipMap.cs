using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public class FLandscapeTexture2DMipMap
{
    public int SizeX;
    public int SizeY;
    public bool bCompressed;
    public readonly FByteBulkData BulkData;

    public FLandscapeTexture2DMipMap(FAssetArchive Ar)
    {
        SizeX = Ar.Read<int>();
        SizeY = Ar.Read<int>();
        bCompressed = Ar.ReadBoolean();

        BulkData = new FByteBulkData(Ar);
    }
}
