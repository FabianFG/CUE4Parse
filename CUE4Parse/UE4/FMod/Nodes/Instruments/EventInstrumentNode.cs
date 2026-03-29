using System.IO;
using CUE4Parse.UE4.FMod.Objects;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.FMod.Nodes.Instruments;

public class EventInstrumentNode : BaseInstrumentNode
{
    [JsonIgnore] public readonly FModGuid BaseGuid;
    public readonly FModGuid EventGuid;
    public readonly float SnapshotIntensity;
    public readonly FEventParameterStub[] EventParameterStubs;

    public EventInstrumentNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        EventGuid = new FModGuid(Ar);
        SnapshotIntensity = Ar.ReadSingle();
        EventParameterStubs = FModReader.ReadElemListImp<FEventParameterStub>(Ar);
    }
}
