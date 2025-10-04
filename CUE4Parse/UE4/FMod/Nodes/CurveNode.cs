using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class CurveNode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid OwnerGuid;
    public readonly FCurvePoint[] CurvePoints;

    public CurveNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        OwnerGuid = new FModGuid(Ar);
        CurvePoints = FModReader.ReadElemListImp<FCurvePoint>(Ar);
    }
}
