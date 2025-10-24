using CUE4Parse.UE4.FMod.Enums;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FParentContext
{
    public readonly ENodeId NodeId;
    public readonly FModGuid Guid;

    public FParentContext(ENodeId nodeId, FModGuid guid)
    {
        NodeId = nodeId;
        Guid = guid;
    }
}
