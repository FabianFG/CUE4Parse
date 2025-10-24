using System.IO;
using CUE4Parse.UE4.FMod.Enums;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Instruments;

public class WaveformInstrumentNode : BaseInstrumentNode
{
    public readonly FModGuid BaseGuid;
    public readonly EWaveformLoadingMode LegacyLoadingMode;
    public readonly FModGuid WaveformResourceGuid;

    public WaveformInstrumentNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        if (FModReader.Version < 0x46)
        {
            LegacyLoadingMode = (EWaveformLoadingMode) Ar.ReadUInt32();
        }
        WaveformResourceGuid = new FModGuid(Ar);
    }
}
