using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Effects;

public class SpectralSideChainEffectNode : BaseEffectNode
{
    public readonly FModGuid BaseGuid;
    public readonly float Level;
    public readonly float MinimumFrequency;
    public readonly float MaximumFrequency;
    public readonly uint Flags;
    public readonly FModGuid[] Targets;

    public SpectralSideChainEffectNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        Level = Ar.ReadSingle();
        MinimumFrequency = Ar.ReadSingle();
        MaximumFrequency = Ar.ReadSingle();
        Flags = Ar.ReadUInt32();
        Ar.ReadBytes(8);
        Targets = FModReader.ReadElemListImp<FModGuid>(Ar);
    }
}
