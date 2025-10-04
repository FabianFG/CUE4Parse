using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes.ModulatorSubnodes;

public class RandomModulatorNode
{
    public readonly float Minimum;
    public readonly float Maximum;
    public readonly float Amount;

    public RandomModulatorNode(BinaryReader Ar)
    {
        Minimum = Ar.ReadSingle();
        Maximum = Ar.ReadSingle();
        Amount = Ar.ReadSingle();
    }
}
