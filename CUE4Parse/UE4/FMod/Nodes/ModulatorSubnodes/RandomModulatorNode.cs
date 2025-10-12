using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes.ModulatorSubnodes;

public class RandomModulatorNode
{
    public readonly float Amount;
    public readonly float Minimum;
    public readonly float Maximum;

    public RandomModulatorNode(BinaryReader Ar)
    {
        if (FModReader.Version >= 0x55)
        {
            Amount = Ar.ReadSingle();
        }
        else
        {
            Minimum = Ar.ReadSingle();
            Maximum = Ar.ReadSingle();
        }
    }
}
