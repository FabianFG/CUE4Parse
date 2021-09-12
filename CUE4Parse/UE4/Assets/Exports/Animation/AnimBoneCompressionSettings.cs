using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimBoneCompressionSettings : UObject
    {
        public FPackageIndex[] Codecs; // UAnimBoneCompressionCodec[]

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Codecs = Get<FPackageIndex[]>(nameof(Codecs));
        }

        public UAnimBoneCompressionCodec? GetCodec(string ddcHandle)
        {
            UAnimBoneCompressionCodec? codecMatch = null;
            foreach (var codec_ in Codecs)
            {
                var codec = codec_.Load<UAnimBoneCompressionCodec>();
                if (codec != null)
                {
                    codecMatch = codec.GetCodec(ddcHandle);

                    if (codecMatch != null)
                    {
                        break; // Found our match
                    }
                }
            }

            return codecMatch;
        }
    }
}