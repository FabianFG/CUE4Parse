using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Effects;

public class SendEffectNode : BaseEffectNode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid ReturnGuid;
    public readonly float SendLevel;

    public SendEffectNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        ReturnGuid = new FModGuid(Ar);
        SendLevel = Ar.ReadSingle();

        if (FModReader.Version >= 0x3D && FModReader.Version <= 0x91)
        {
            bool legacyBypass = Ar.ReadBoolean();
        }
    }
}
