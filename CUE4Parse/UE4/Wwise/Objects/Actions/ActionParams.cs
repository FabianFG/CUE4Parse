using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class ActionParams
{
    public int? TTime { get; private set; }
    public int? TTimeMin { get; private set; }
    public int? TTimeMax { get; private set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public ECurveInterpolation FadeCurve { get; private set; }

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
