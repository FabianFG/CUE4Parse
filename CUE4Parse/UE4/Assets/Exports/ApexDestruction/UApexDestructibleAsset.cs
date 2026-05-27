using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.ApexDestruction;

public class UApexClothingAsset : UApexDestructibleAsset;
public class UApexDestructibleAsset : UObject
{
    public byte[]? NameBuffer;
    public byte[]? NxDestructibleAssetBuffer;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var bAssetValid = Ar.ReadBoolean();

        if (bAssetValid)
        {
            NameBuffer = Ar.ReadArray<byte>();
            NxDestructibleAssetBuffer = Ar.ReadArray<byte>();
        }
    }
}