using System.Text;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkMusicMarkerWwise
{
    public readonly uint Id;
    public readonly double Position;
    public readonly string? MarkerName;

    public AkMusicMarkerWwise(FWwiseArchive Ar)
    {
        Id = Ar.Read<uint>();
        Position = Ar.Read<double>();
        MarkerName = Ar.Version switch
        {
            <= 62 => null,
            <= 136 => Encoding.ASCII.GetString(Ar.ReadArray<byte>()).TrimEnd('\0'),
            _ => Ar.ReadStzString(),
        };
    }

    public static AkMusicMarkerWwise[] ReadArray(FWwiseArchive Ar) =>
        Ar.ReadArray((int) Ar.Read<uint>(), () => new AkMusicMarkerWwise(Ar));
}
