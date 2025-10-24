using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class ControllerNode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid PropertyOwnerGuid;
    public readonly FModGuid CurveGuid;
    public readonly int PropertyIndex;

    public ControllerNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        PropertyOwnerGuid = new FModGuid(Ar);
        if (FModReader.Version < 0x5a) new FModGuid(Ar);
        CurveGuid = new FModGuid(Ar);
        PropertyIndex = Ar.ReadInt32();
    }
}
