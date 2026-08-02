using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public abstract class UAnimSequenceBase : UAnimationAsset
    {
        public FAnimNotifyEvent[]? Notifies;
        public float SequenceLength;
        public FRawCurveTracks? RawCurveData;
        public float RateScale;
        public bool bLoop;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            Notifies = GetOrDefault<FAnimNotifyEvent[]?>(nameof(Notifies));
            SequenceLength = GetOrDefault<float>(nameof(SequenceLength));
            RawCurveData = GetOrDefault<FRawCurveTracks?>(nameof(RawCurveData));
            RateScale = GetOrDefault(nameof(RateScale), 1.0f);
            bLoop = GetOrDefault<bool>(nameof(bLoop));
        }
    }
}
