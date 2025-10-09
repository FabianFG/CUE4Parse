using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FTriggerBox
{
    public readonly FModGuid Guid;
    public readonly uint E;       // offset 0x10
    public readonly uint F;       // offset 0x14

    public FTriggerBox(BinaryReader Ar)
    {
        Guid = new FModGuid(Ar);
        E = Ar.ReadUInt32();
        F = Ar.ReadUInt32();
    }
}
