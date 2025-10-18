using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Effects;

public class PluginEffectNode : BaseEffectNode
{
    public readonly FModGuid BaseGuid;
    public readonly string PluginName;
    public readonly string Name = string.Empty;
    public ParameterizedEffectNode? ParamEffectBody;

    public PluginEffectNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        if (FModReader.Version < 0x5b) Ar.ReadUInt32(); // Legacy InputChannelLayout

        PluginName = FModReader.ReadString(Ar);
        if (FModReader.Version >= 0x36) Name = FModReader.ReadString(Ar);

        if (FModReader.Version >= 0x3d && FModReader.Version <= 0x91)
        {
            bool legacyBypass = Ar.ReadBoolean();
        }
    }
}
