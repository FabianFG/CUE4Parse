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
