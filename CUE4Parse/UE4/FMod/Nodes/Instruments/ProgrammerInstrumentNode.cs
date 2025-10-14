using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Instruments;

public class ProgrammerInstrumentNode : BaseInstrumentNode
{
    public readonly FModGuid BaseGuid;
    public readonly string Name = string.Empty;

    public ProgrammerInstrumentNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        Name = FModReader.ReadString(Ar);
    }
}
