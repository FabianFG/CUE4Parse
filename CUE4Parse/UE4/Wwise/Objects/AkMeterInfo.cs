using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkMeterInfo
{
    public double GridPeriod { get; private set; }
    public double GridOffset { get; private set; }
    public float Tempo { get; private set; }
    public byte TimeSigNumBeatsBar { get; private set; }
    public byte TimeSigBeatValue { get; private set; }
    public byte MeterInfoFlag { get; private set; }

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
