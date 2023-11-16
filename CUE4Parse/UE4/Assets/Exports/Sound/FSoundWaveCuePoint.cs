using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    public class FSoundWaveCuePoint
    {
        public readonly string Label;

        public FSoundWaveCuePoint(FAssetArchive Ar)
        {
            Label = Ar.ReadFString();
        }
    }
}
