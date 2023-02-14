using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimComposite : UAnimCompositeBase
    {
        public FAnimTrack AnimationTrack;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            AnimationTrack = GetOrDefault<FAnimTrack>(nameof(AnimationTrack));
        }
    }
}
