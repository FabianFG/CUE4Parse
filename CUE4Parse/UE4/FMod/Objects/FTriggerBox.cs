using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FTriggerBox
{
    public readonly FModGuid Guid;
    public readonly uint StartTime;
    public readonly uint Length;

    public FTriggerBox(BinaryReader Ar)
    {
        Guid = new FModGuid(Ar);
        StartTime = Ar.ReadUInt32();
        Length = Ar.ReadUInt32();
    }
}
