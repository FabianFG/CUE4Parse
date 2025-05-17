using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkDuckInfo
{
    public uint BusId { get; set; }
    public float DuckVolume { get; set; }
    public uint FadeOutTime { get; set; }
    public uint FadeInTime { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public ECurveInterpolation FadeCurve { get; set; }
    public uint DuckingStateType { get; set; }
    public byte TargetProp { get; set; } // Version > 65

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
