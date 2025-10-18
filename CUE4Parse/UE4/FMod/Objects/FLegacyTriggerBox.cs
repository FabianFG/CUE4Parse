using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FLegacyTriggerBox
{
    public readonly FModGuid InstrumentGuid;
    public readonly float Position;
    public readonly float Length;

    public FLegacyTriggerBox(BinaryReader Ar)
    {
        InstrumentGuid = new FModGuid(Ar);
        Position = Ar.ReadSingle();
        Length = Ar.ReadSingle();
    }
}
