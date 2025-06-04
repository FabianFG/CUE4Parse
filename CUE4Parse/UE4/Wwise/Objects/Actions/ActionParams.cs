using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class ActionParams
{
    public readonly int TTime;
    public readonly int TTimeMin;
    public readonly int TTimeMax;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly ECurveInterpolation FadeCurve;

    public ActionParams(FArchive Ar)
    {
        if (WwiseVersions.Version <= 56)
        {
            TTime = Ar.Read<int>();
            TTimeMin = Ar.Read<int>();
            TTimeMax = Ar.Read<int>();
        }

        var byBitVector = Ar.Read<byte>();
        FadeCurve = (ECurveInterpolation) (byBitVector & 0x1F);
    }
}
