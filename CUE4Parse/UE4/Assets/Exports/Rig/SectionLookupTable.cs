using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class SectionLookupTable
{
    public uint Descriptor;
    public uint Definition;
    public uint Behaviour;
    public uint Controls;
    public uint Joints;
    public uint BlendShapeChannels;
    public uint AnimatedMaps;
    public uint Geometry;

    public SectionLookupTable(FArchiveBigEndian Ar)
    {
        Descriptor = Ar.Read<uint>();
        Definition = Ar.Read<uint>();
        Behaviour = Ar.Read<uint>();
        Controls = Ar.Read<uint>();
        Joints = Ar.Read<uint>();
        BlendShapeChannels = Ar.Read<uint>();
        AnimatedMaps = Ar.Read<uint>();
        Geometry = Ar.Read<uint>();
    }
}
