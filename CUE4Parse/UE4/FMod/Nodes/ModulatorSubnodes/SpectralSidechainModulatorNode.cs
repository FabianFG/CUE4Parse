using System.IO;
using CUE4Parse.UE4.FMod.Objects;
using CUE4Parse.UE4.FMod.Enums;

namespace CUE4Parse.UE4.FMod.Nodes.ModulatorSubnodes;

public class SpectralSidechainModulatorNode
{
    public readonly float Amount;
    public readonly ESpectralSidechainModulatorMode Mode;
    public readonly float ThresholdMinimum;
    public readonly float ThresholdMaximum;
    public readonly float AttackTime;
    public readonly float ReleaseTime;
    public readonly FModGuid ThresholdMapping;

    public SpectralSidechainModulatorNode(BinaryReader Ar)
    {
        Amount = Ar.ReadSingle();
        Mode = (ESpectralSidechainModulatorMode) Ar.ReadInt32();
        ThresholdMinimum = Ar.ReadSingle();
        ThresholdMaximum = Ar.ReadSingle();
        AttackTime = Ar.ReadSingle();
        ReleaseTime = Ar.ReadSingle();

        ThresholdMapping = new FModGuid(Ar);
    }
}
