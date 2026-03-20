using System.IO;
using CUE4Parse.UE4.FMod.Objects;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.FMod.Nodes.Instruments;

public class ProgrammerInstrumentNode : BaseInstrumentNode
{
    [JsonIgnore] public readonly FModGuid BaseGuid;
    public readonly string Name;

    public ProgrammerInstrumentNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        Name = FModReader.ReadString(Ar);
    }
}
