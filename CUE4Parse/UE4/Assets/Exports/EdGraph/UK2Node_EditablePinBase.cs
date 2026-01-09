using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine.EdGraph;

namespace CUE4Parse.UE4.Assets.Exports.EdGraph;

public class UK2Node : UEdGraphNode;

public class UK2Node_EditablePinBase : UK2Node
{
    public FUserPinInfo[]? SerializedItems;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        SerializedItems = Ar.ReadArray(() => new FUserPinInfo(Ar));
    }
}
