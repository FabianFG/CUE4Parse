using System.IO;
using CUE4Parse.UE4.FMod.Objects;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.FMod.Nodes.Instruments;

public class CommandInstrumentNode : BaseInstrumentNode
{
    [JsonIgnore] public readonly FModGuid BaseGuid;
    public readonly uint CommandType;
    public readonly FModGuid TargetGuid;
    public readonly float Value;

    public CommandInstrumentNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        CommandType = Ar.ReadUInt32();
        TargetGuid = new FModGuid(Ar);

        if (FModReader.Version >= 0x80)
        {
            Value = Ar.ReadSingle();
        }
    }
}
