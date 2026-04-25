using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionParams
{
    public readonly int TTime;
    public readonly int TTimeMin;
    public readonly int TTimeMax;
    public readonly EAkCurveInterpolation FadeCurve;

    public CAkActionParams(FWwiseArchive Ar)
    {
        if (Ar.Version <= 56)
        {
            TTime = Ar.Read<int>();
            TTimeMin = Ar.Read<int>();
            TTimeMax = Ar.Read<int>();
        }

        var byBitVector = Ar.Read<byte>();
        FadeCurve = (EAkCurveInterpolation) (byBitVector & 0x1F);
    }
}
