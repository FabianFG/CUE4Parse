using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkDuckInfo
{
    public uint BusId { get; set; }
    public uint DuckVolume { get; set; }
    public uint FadeOutTime { get; set; }
    public uint FadeInTime { get; set; }
    public ECurveInterpolation FadeCurve { get; set; }
    public uint DuckingStateType { get; set; }
    public byte TargetProp { get; set; } // Version > 65

    public AkDuckInfo(FArchive Ar)
    {
        BusId = Ar.Read<uint>();
        DuckVolume = Ar.Read<uint>();
        FadeOutTime = Ar.Read<uint>();
        FadeInTime = Ar.Read<uint>();

        var byBitVector = Ar.Read<byte>();
        FadeCurve = (ECurveInterpolation) (byBitVector & 0x1F);
        if (WwiseVersions.WwiseVersion > 65)
        {
            TargetProp = Ar.Read<byte>();
        }
    }
}
