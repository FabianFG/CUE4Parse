using CUE4Parse.UE4.FMod.Objects;
using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes.Effects;

public class ParameterizedEffectNode
{
    public readonly FEffectParameter[] Parameters;
    public readonly bool SideChainEnabled;

    public ParameterizedEffectNode(BinaryReader Ar)
    {
        int paramCount = Ar.ReadInt32();
        Parameters = new FEffectParameter[paramCount];
        for (int i = 0; i < paramCount; i++)
        {
            Parameters[i] = new FEffectParameter(Ar);
        }

        if (FModReader.Version >= 0x6e)
        {
            SideChainEnabled = Ar.ReadBoolean();
        }
    }
}
