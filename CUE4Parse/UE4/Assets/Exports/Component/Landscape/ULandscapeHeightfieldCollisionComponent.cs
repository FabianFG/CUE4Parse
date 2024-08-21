using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public class ULandscapeHeightfieldCollisionComponent : USceneComponent
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        var bCooked = Ar.ReadBoolean();
        if (bCooked)
        {
            Ar.SkipBulkArrayData(); // CookedCollisionData
        }
    }
}
