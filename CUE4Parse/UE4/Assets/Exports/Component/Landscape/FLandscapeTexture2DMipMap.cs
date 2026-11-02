using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public enum ELandscapeTextureStorageFormat : byte
{
    HeightsWithEdgeNormals = 0,
    HeightsAndNormals = 1,
}

public class FLandscapeTexture2DMipMap
{
    public int SizeX;
    public int SizeY;
    public bool bCompressed;
    public ELandscapeTextureStorageFormat StorageFormat = ELandscapeTextureStorageFormat.HeightsWithEdgeNormals;
    public readonly FByteBulkData BulkData;

    public FLandscapeTexture2DMipMap(FAssetArchive Ar)
    {
        SizeX = Ar.Read<int>();
        SizeY = Ar.Read<int>();
        bCompressed = Ar.ReadBoolean();
        if (Ar.Game >= GAME_UE6_0)
        {
            StorageFormat = Ar.Read<ELandscapeTextureStorageFormat>();
        }

        BulkData = new FByteBulkData(Ar);
    }
}
