using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes.ModulatorSubnodes;

public class LFOModulatorNode
{
    public readonly uint Shape;
    public readonly uint Flags;
    public readonly float Rate;
    public readonly float Amount;
    public readonly float Phase;
    public readonly float Direction;

    public LFOModulatorNode(BinaryReader Ar)
    {
        Shape = Ar.ReadUInt32();      // 0x50
        Flags = Ar.ReadUInt32();      // 0x54
        Rate = Ar.ReadSingle();       // 0x58
        Amount = Ar.ReadSingle();     // 0x5C
        Phase = Ar.ReadSingle();      // 0x60
        Direction = Ar.ReadSingle();  // 0x64
    }
}
