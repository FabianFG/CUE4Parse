using System.Text;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkMusicMarkerWwise
{
    public readonly uint Id;
    public readonly double Position;
    public readonly string? MarkerName;

    public AkMusicMarkerWwise(FArchive Ar)
    {
        Id = Ar.Read<uint>();
        Position = Ar.Read<double>();
        MarkerName = WwiseVersions.Version switch
        {
            <= 62 => null,
            <= 136 => Encoding.ASCII.GetString(Ar.ReadArray<byte>()).TrimEnd('\0'),
            _ => WwiseReader.ReadStzString(Ar),
        };
    }

    public static AkMusicMarkerWwise[] ReadArray(FArchive Ar) =>
        Ar.ReadArray((int) Ar.Read<uint>(), () => new AkMusicMarkerWwise(Ar));
}
