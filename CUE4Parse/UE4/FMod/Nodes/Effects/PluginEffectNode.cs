using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Effects;

public class PluginEffectNode : BaseEffectNode
{
    public readonly FModGuid BaseGuid;
    public readonly string PluginName;
    public readonly string Name;
    public ParameterizedEffectNode? ParamEffectBody;

    public PluginEffectNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        PluginName = FModReader.ReadString(Ar);
        Name = FModReader.ReadString(Ar);

        bool legacyBypass = Ar.ReadBoolean();
    }
}
