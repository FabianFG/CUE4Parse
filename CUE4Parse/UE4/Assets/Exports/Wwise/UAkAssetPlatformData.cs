using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Wwise
{
    public class UAkAssetPlatformData : UObject
    {
        public FPackageIndex? CurrentAssetData;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            CurrentAssetData = new FPackageIndex(Ar);
        }
    }
}
