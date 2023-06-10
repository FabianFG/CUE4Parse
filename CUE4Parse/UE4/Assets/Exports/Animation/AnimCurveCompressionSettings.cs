using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class UAnimCurveCompressionSettings : UObject
{
    public FPackageIndex? Codec; // UAnimCurveCompressionCodec

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Codec = GetOrDefault<FPackageIndex>(nameof(Codec));
    }

    public UAnimCurveCompressionCodec? GetCodec(string path) => Codec?.Load<UAnimCurveCompressionCodec>()?.GetCodec(path);
}
