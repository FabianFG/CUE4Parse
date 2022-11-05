using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Engine.Font;

public class UFontBulkData : UObject
{
    public FByteBulkData? BulkData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        BulkData = new FByteBulkData(Ar);
    }
}
