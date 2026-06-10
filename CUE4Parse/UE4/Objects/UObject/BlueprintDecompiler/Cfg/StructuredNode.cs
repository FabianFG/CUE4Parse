using System.Collections.Generic;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

internal abstract class StructuredNode;

internal sealed class SeqNode : StructuredNode
{
    public readonly List<StructuredNode> Children;

    public SeqNode(List<StructuredNode> children)
    {
        Children = children;
    }
}

internal enum TermKind
{
    FallThrough,
    Return,
    Exit,
    Goto,
    If
}

internal sealed class BlockNode : StructuredNode
{
    public readonly int Block;
    public TermKind Kind;
    public int GotoTarget = -1;
    public StructuredNode? Then;
    public StructuredNode? Else;

    public BlockNode(int block, TermKind kind)
    {
        Block = block;
        Kind = kind;
    }
}

internal sealed class GotoNode : StructuredNode
{
    public readonly int Target;

    public GotoNode(int target)
    {
        Target = target;
    }
}

internal sealed class LeafRangeNode : StructuredNode
{
    public readonly int Block;
    public readonly int LeafEnd;

    public LeafRangeNode(int block, int leafEnd)
    {
        Block = block;
        LeafEnd = leafEnd;
    }
}

internal sealed class ComputedGotoNode : StructuredNode
{
    public readonly int Block;
    public readonly List<int> Entries;

    public ComputedGotoNode(int block, List<int> entries)
    {
        Block = block;
        Entries = entries;
    }
}

internal sealed class LoopNode : StructuredNode
{
    public readonly int Header;
    public readonly StructuredNode Body;

    public LoopNode(int header, StructuredNode body)
    {
        Header = header;
        Body = body;
    }
}

internal sealed class ReturnNode : StructuredNode;

internal sealed class BreakNode : StructuredNode;

internal sealed class ContinueNode : StructuredNode;

internal sealed class SwitchCase
{
    public readonly string Label;
    public readonly StructuredNode Body;

    public SwitchCase(string label, StructuredNode body)
    {
        Label = label;
        Body = body;
    }
}

internal sealed class SwitchNode : StructuredNode
{
    public readonly string Subject;
    public readonly List<SwitchCase> Cases;
    public readonly StructuredNode? Default;

    public SwitchNode(string subject, List<SwitchCase> cases, StructuredNode? @default)
    {
        Subject = subject;
        Cases = cases;
        Default = @default;
    }
}
