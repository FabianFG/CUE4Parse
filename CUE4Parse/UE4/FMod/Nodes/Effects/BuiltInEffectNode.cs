using System.IO;
using CUE4Parse.UE4.FMod.Enums;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Effects;

public class BuiltInEffectNode : BaseEffectNode
{
    public readonly FModGuid BaseGuid;
    public readonly EDSPType DSPType;
    public ParameterizedEffectNode? ParamEffectBody;

    public BuiltInEffectNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        DSPType = (EDSPType)Ar.ReadUInt32();

        if (FModReader.Version >= 0x3D && FModReader.Version <= 0x91)
        {
            bool legacyBypass = Ar.ReadBoolean();
        }
    }
}
