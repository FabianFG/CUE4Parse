using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

// AkDecisionTree::SetTree
// Used in AkDecisionTree::ResolvePath, AkDecisionTree::ResolvePathWeighted
public class AkDecisionTree
{
    public readonly AkDecisionTreeNode[] Nodes;

    public AkDecisionTree()
    {
        Nodes = [];
    }

    public AkDecisionTree(FArchive Ar, uint treeDepth, uint treeDataSize)
    {
        Nodes = ParseDecisionTree(Ar, treeDataSize, treeDepth);
    }

    private AkDecisionTreeNode[] ParseDecisionTree(FArchive Ar, uint size, uint maxDepth)
    {
        uint itemSize = DetermineItemSize();
        uint countMax = size / itemSize;

        var nodes = new AkDecisionTreeNode[1];
        ParseTreeNode(Ar, nodes, 1, countMax, 0, (int) maxDepth, (int) itemSize, null);

        return nodes;
    }

    private static uint DetermineItemSize()
    {
        return WwiseVersions.Version switch
        {
            <= 29 => 0x08,
            <= 36 => 0x0C,
            <= 45 => 0x08,
            _ => 0x0C, // Default
        };
    }

    private static void ParseTreeNode(FArchive Ar, AkDecisionTreeNode[] nodes, uint count, uint countMax, int curDepth, int maxDepth, int itemSize, AkDecisionTreeNode? parent)
    {
        var parsedNodes = new AkDecisionTreeNode[count];
        for (int i = 0; i < count; i++)
        {
            var node = new AkDecisionTreeNode(Ar, countMax, curDepth, maxDepth, itemSize);
            if (parent == null)
            {
                nodes[i] = node;
            }
            else
            {
                parent.Children[i] = node;
            }

            parsedNodes[i] = node;
        }

        foreach (var node in parsedNodes)
        {
            if (node.ChildrenCount > 0)
            {
                ParseTreeNode(Ar, nodes, node.ChildrenCount, countMax, curDepth + 1, maxDepth, itemSize, node);
            }
        }
    }
}

public class AkDecisionTreeNode
{
    public readonly uint Key;
    public readonly uint AudioNodeId;
    public readonly ushort ChildrenIndex;
    public readonly ushort ChildrenCount;
    public readonly ushort Weight;
    public readonly ushort Probability;
    public readonly AkDecisionTreeNode[] Children;

    public AkDecisionTreeNode(FArchive Ar, uint countMax, int currentDepth, int maxDepth, int itemSize)
    {
        Key = Ar.Read<uint>();

        bool isAudioNode = IsAudioNode(Ar, countMax, itemSize);
        if (isAudioNode || currentDepth == maxDepth)
        {
            AudioNodeId = Ar.Read<uint>();
            ChildrenCount = 0;
        }
        else
        {
            ChildrenIndex = Ar.Read<ushort>();
            ChildrenCount = Ar.Read<ushort>();
        }

        Children = ChildrenCount > 0 ? new AkDecisionTreeNode[ChildrenCount] : [];

        if (WwiseVersions.Version > 29 && WwiseVersions.Version <= 36)
        {
            Weight = Ar.Read<ushort>();
            Probability = Ar.Read<ushort>();
        }
        else if (WwiseVersions.Version > 45)
        {
            Weight = Ar.Read<ushort>();
            Probability = Ar.Read<ushort>();
        }
    }

    private static bool IsAudioNode(FArchive Ar, uint countMax, int itemSize)
    {
        long originalPosition = Ar.Position;

        uint idCh = Ar.Read<uint>();
        ushort uIndex = (ushort) (idCh & 0xFFFF);
        ushort uCount = (ushort) ((idCh >> 16) & 0xFFFF);

        Ar.Position = originalPosition;

        bool isIdInvalid = uIndex > countMax || uCount > countMax;
        bool isOverBounds = Ar.Position + uCount * itemSize > Ar.Length;

        return isIdInvalid || isOverBounds;
    }
}
