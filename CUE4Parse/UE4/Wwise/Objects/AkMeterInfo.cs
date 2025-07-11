using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkMeterInfo
{
    public readonly double GridPeriod;
    public readonly double GridOffset;
    public readonly float Tempo;
    public readonly byte TimeSigNumBeatsBar;
    public readonly byte TimeSigBeatValue;
    public readonly byte MeterInfoFlag;

    public AkMeterInfo(FArchive Ar)
    {
        GridPeriod = Ar.Read<double>();
        GridOffset = Ar.Read<double>();
        Tempo = Ar.Read<float>();
        TimeSigNumBeatsBar = Ar.Read<byte>();
        TimeSigBeatValue = Ar.Read<byte>();
        MeterInfoFlag = Ar.Read<byte>();
    }
}
