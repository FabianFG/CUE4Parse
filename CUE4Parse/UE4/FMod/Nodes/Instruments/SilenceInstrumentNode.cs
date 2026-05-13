using System.IO;
using CUE4Parse.UE4.FMod.Objects;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.FMod.Nodes.Instruments;

public class SilenceInstrumentNode : BaseInstrumentNode
{
    [JsonIgnore] public readonly FModGuid BaseGuid;
    public readonly float Duration;

    public SilenceInstrumentNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        Duration = Ar.ReadSingle();
    }
}
