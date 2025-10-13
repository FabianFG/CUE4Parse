using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class ParameterLayoutNode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid ParameterGuid;
    public readonly FModGuid[] Instruments;
    public readonly uint Flags;
    public readonly FModGuid[] Controllers;
    public readonly FModGuid[] TriggerBoxes;

    public ParameterLayoutNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        ParameterGuid = new FModGuid(Ar);
        Instruments = FModReader.ReadElemListImp<FModGuid>(Ar);
        Controllers = FModReader.ReadElemListImp<FModGuid>(Ar);
        TriggerBoxes = FModReader.ReadElemListImp<FModGuid>(Ar);
    }
}
