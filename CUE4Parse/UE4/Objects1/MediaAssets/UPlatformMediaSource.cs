using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.MediaAssets
{
    public class UPlatformMediaSource : UMediaSource
    {
        public FPackageIndex? MediaSource; // UMediaSource

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            MediaSource = new FPackageIndex(Ar);
        }
    }
}
