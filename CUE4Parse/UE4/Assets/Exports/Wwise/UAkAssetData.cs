using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Wwise
{
    public class UAkAssetData : UObject
    {
        public FByteBulkData? Data;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            Data = new FByteBulkData(Ar);
        }
    }
}
