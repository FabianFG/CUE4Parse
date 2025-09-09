using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkDuckInfo
{
    public readonly uint BusId;
    public readonly float DuckVolume;
    public readonly uint FadeOutTime;
    public readonly uint FadeInTime;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly ECurveInterpolation FadeCurve;
    public readonly byte TargetProp; // Version > 65

    public AkDuckInfo(FArchive Ar)
    {
        BusId = Ar.Read<uint>();
        DuckVolume = Ar.Read<float>();
        FadeOutTime = Ar.Read<uint>();
        FadeInTime = Ar.Read<uint>();

        var byBitVector = Ar.Read<byte>();
        FadeCurve = (ECurveInterpolation) (byBitVector & 0x1F);
        if (WwiseVersions.Version > 65)
        {
            TargetProp = Ar.Read<byte>();
        }
    }
}
