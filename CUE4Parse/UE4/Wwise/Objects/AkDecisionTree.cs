using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkDecisionTree
{
    public List<AkDecisionTreeNode> Nodes { get; private set; }

    public AkDecisionTree()
    {
        Nodes = [];
    }

    public AkDecisionTree(FArchive Ar, uint treeDepth, uint treeDataSize)
    {
        Nodes = ParseDecisionTree(Ar, treeDataSize, treeDepth);
    }

    private List<AkDecisionTreeNode> ParseDecisionTree(FArchive Ar, uint size, uint maxDepth)
    {
        var nodes = new List<AkDecisionTreeNode>();
        uint itemSize = DetermineItemSize();
        uint countMax = size / itemSize;

        ParseTreeNode(Ar, nodes, 1, countMax, 0, (int) maxDepth, (int) itemSize, null);

        return nodes;
    }

    private static uint DetermineItemSize()
    {
        if (WwiseVersions.Version <= 29)
            return 0x08;
        if (WwiseVersions.Version <= 36)
            return 0x0C;
        if (WwiseVersions.Version <= 45)
            return 0x08;
        return 0x0C; // Default
    }

    private static void ParseTreeNode(FArchive Ar, List<AkDecisionTreeNode> nodes, uint count, uint countMax, int curDepth, int maxDepth, int itemSize, AkDecisionTreeNode? parent)
    {
        var parsedNodes = new List<(AkDecisionTreeNode Node, int ChildrenCount)>();

        for (int i = 0; i < count; i++)
        {
            var node = new AkDecisionTreeNode(Ar, countMax, curDepth, maxDepth, itemSize);

            //Log.Warning($"Parsing Node - Key: {node.Key}, AudioNodeId: {node.AudioNodeId}, ChildrenIndex: {node.ChildrenIndex}, ChildrenCount: {node.ChildrenCount}");

            if (parent == null)
            {
                nodes.Add(node);
            }
            else
            {
                parent.Children.Add(node);
            }

            parsedNodes.Add((node, node.ChildrenCount));
        }

        foreach (var (node, childrenCount) in parsedNodes)
        {
            if (childrenCount > 0)
            {
                ParseTreeNode(Ar, nodes, (uint) childrenCount, countMax, curDepth + 1, maxDepth, itemSize, node);
            }
        }
    }
}

public class AkDecisionTreeNode
{
    public uint Key { get; private set; }
    public uint AudioNodeId { get; private set; }
    public ushort ChildrenIndex { get; private set; }
    public ushort ChildrenCount { get; private set; }
    public ushort Weight { get; private set; }
    public ushort Probability { get; private set; }
    public List<AkDecisionTreeNode> Children { get; private set; }

    public AkDecisionTreeNode(FArchive Ar, uint countMax, int currentDepth, int maxDepth, int itemSize)
    {
        Key = Ar.Read<uint>();
        Children = [];

        bool isAudioNode = IsAudioNode(Ar, countMax, currentDepth, maxDepth, itemSize);

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

    private static bool IsAudioNode(FArchive Ar, uint countMax, int currentDepth, int maxDepth, int itemSize)
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
