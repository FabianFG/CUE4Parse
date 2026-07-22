using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DeadByDaylight.Objects;

public class FBhvrBarkNodeTemplate : IUStruct
{
    public FPackageIndex? NodeType;
    public FStructFallback? Node;

    public FBhvrBarkNodeTemplate() { }
    public FBhvrBarkNodeTemplate(FAssetArchive Ar)
    {
        if (!Ar.ReadBoolean()) return;

        NodeType = new FPackageIndex(Ar);

        if (NodeType.IsNull) return;

        Node = new FStructFallback(Ar, NodeType.Name);
    }
}
