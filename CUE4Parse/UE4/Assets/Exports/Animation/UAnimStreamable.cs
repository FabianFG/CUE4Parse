using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class UAnimStreamable : UAnimSequenceBase
{ 
    public int NumFrames; 
    public FName RetargetSource; 
    public UAnimCurveCompressionSettings CurveCompressionSettings; 
    public FStructFallback RawCurveData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        
        NumFrames = GetOrDefault(nameof(NumFrames), 0);
        RetargetSource = GetOrDefault<FName>(nameof(RetargetSource));
        CurveCompressionSettings = GetOrDefault<UAnimCurveCompressionSettings>(nameof(CurveCompressionSettings));
        RawCurveData = GetOrDefault<FStructFallback>(nameof(RawCurveData));
    }
}