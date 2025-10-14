using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes.ModulatorSubnodes;

public class SeekModulatorNode
{
    public readonly uint Flags;
    public readonly float SeekSpeedAscending;
    public readonly float SeekSpeedDescending;

    public SeekModulatorNode(BinaryReader Ar)
    {
        Flags = Ar.ReadUInt32();
        SeekSpeedAscending = Ar.ReadSingle();
        SeekSpeedDescending = Ar.ReadSingle();
    }
}
