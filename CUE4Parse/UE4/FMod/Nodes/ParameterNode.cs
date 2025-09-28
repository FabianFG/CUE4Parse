using System.IO;
using CUE4Parse.UE4.FMod.Objects;
using CUE4Parse.UE4.FMod.Enums;

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

    public ParameterNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        Flags = Ar.ReadInt32();

        var type = EFModStudioParameterType.FMOD_STUDIO_PARAMETER_AUTOMATIC_DIRECTION;

        if (FModReader.Version >= 0x70)
        {
            type = (EFModStudioParameterType) Ar.ReadUInt32();

            Name = FModReader.ReadSerializedString(Ar);
            Minimum = Ar.ReadSingle();
            Maximum = Ar.ReadSingle();
            DefaultValue = Ar.ReadSingle();
            Velocity = Ar.ReadSingle();
            SeekSpeed = Ar.ReadSingle();

            // TODO: more to read 
        }

        Type = type;
    }
}
