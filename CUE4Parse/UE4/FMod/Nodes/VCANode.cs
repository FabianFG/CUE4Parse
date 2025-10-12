using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class VCANode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid[] Strips;
    public readonly FMixerStrip MixerStrip;

    public VCANode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        Strips = FModReader.ReadElemListImp<FModGuid>(Ar);
        MixerStrip = new FMixerStrip(Ar);
    }
}
