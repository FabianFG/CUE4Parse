using System.IO;
using CUE4Parse.UE4.FMod.Objects;
using CUE4Parse.UE4.FMod.Enums;

namespace CUE4Parse.UE4.FMod.Nodes;

public class WaveformResourceNode
{
    public readonly FModGuid BaseGuid;
    public readonly int SubsoundIndex;
    public readonly int SoundBankIndex;
    public readonly EWaveformLoadingMode LoadingMode;

    public WaveformResourceNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        Ar.ReadBytes(2); // Unknown bytes
        SubsoundIndex = Ar.ReadInt32();
        SoundBankIndex = Ar.ReadInt32();
        if (FModReader.Version >= 0x46)
        {
            LoadingMode = (EWaveformLoadingMode) Ar.ReadUInt32();
        }
    }
}
