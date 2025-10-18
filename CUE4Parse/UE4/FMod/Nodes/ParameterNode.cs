using System.IO;
using CUE4Parse.UE4.FMod.Enums;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class ParameterNode
{
    public readonly FModGuid BaseGuid;
    public readonly int Flags;
    public readonly EFModStudioParameterType Type;
    public readonly string Name = string.Empty;
    public readonly float Minimum;
    public readonly float Maximum;
    public readonly float DefaultValue;
    public readonly float Velocity;
    public readonly float SeekSpeed;
    public readonly float SeekSpeedDown;
    public readonly string[] Labels;

    public ParameterNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        if (FModReader.Version >= 0x70) Flags = Ar.ReadInt32();
        if (FModReader.Version < 0x70) Ar.ReadBoolean();

        Type = (EFModStudioParameterType) Ar.ReadUInt32();
        Name = FModReader.ReadString(Ar);
        Minimum = Ar.ReadSingle();
        Maximum = Ar.ReadSingle();
        DefaultValue = Ar.ReadSingle();
        Velocity = Ar.ReadSingle();

        if (FModReader.Version < 0x8f) SeekSpeed = Ar.ReadSingle();
        if (FModReader.Version < 0x70) Ar.ReadBoolean();
        if (FModReader.Version >= 0x52 && FModReader.Version <= 0x8E) SeekSpeedDown = Ar.ReadSingle();
        if (FModReader.Version < 0x60) FModReader.ReadElemListImp<FModGuid>(Ar);

        Labels = FModReader.Version >= 0x8b ? FModReader.ReadVersionedElemListImp(Ar, FModReader.ReadString) : [];
    }
}
