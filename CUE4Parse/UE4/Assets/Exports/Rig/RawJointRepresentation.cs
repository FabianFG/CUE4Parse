using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawJointRepresentation
{
    public ushort Translation;
    public ushort Rotation;
    public ushort Scale;

    public RawJointRepresentation(FArchiveBigEndian Ar)
    {
        Translation = Ar.Read<ushort>();
        Rotation = Ar.Read<ushort>();
        Scale = Ar.Read<ushort>();
    }
}
