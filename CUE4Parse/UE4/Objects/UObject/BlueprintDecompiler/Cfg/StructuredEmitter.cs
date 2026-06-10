using System.Collections.Generic;
using CUE4Parse.UE4.Kismet;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

internal sealed class StructuredEmitter
{
    private readonly ControlFlowGraph _cfg;
    private readonly HashSet<int> _gotoTargets;
    private readonly CustomStringBuilder _builder;
    private bool _fresh = true;

    public StructuredEmitter(ControlFlowGraph cfg, HashSet<int> gotoTargets, CustomStringBuilder builder)
    {
        _cfg = cfg;
        _gotoTargets = gotoTargets;
        _builder = builder;
    }

    public void Emit(StructuredNode node)
    {
        switch (node)
        {
            case SeqNode seq:
                foreach (var child in seq.Children)
                    Emit(child);
                break;
            case GotoNode jump:
                Line($"goto Label_{_cfg.LabelNumber(jump.Target)};");
                break;
            case ReturnNode:
                Line("return;");
                break;
            case BreakNode:
                Line("break;");
                break;
            case ContinueNode:
                Line("continue;");
                break;
            case LoopNode loop:
                EmitLoop(loop);
                break;
            case SwitchNode switchNode:
                EmitSwitch(switchNode);
                break;
            case LeafRangeNode range:
                EmitLeaves(_cfg.Blocks[range.Block], _gotoTargets.Contains(range.Block) ? $"Label_{_cfg.LabelNumber(range.Block)}:" : null, range.LeafEnd);
                break;
            case BlockNode block:
                EmitBlock(block);
                break;
        }
    }

    private void EmitSwitch(SwitchNode node)
    {
        Line($"switch ({node.Subject})");
        OpenBraces();
        foreach (var switchCase in node.Cases)
        {
            Line($"case {switchCase.Label}:");
            _builder.IncreaseIndentation();
            _fresh = true;
            Emit(switchCase.Body);
            if (!AlwaysTerminates(switchCase.Body))
                Line("break;");
            _builder.DecreaseIndentation();
        }
        if (node.Default is not null && !IsEmptyNode(node.Default))
        {
            Line("default:");
            _builder.IncreaseIndentation();
            _fresh = true;
            Emit(node.Default);
            _builder.DecreaseIndentation();
        }
        CloseBraces();
    }

    private bool IsEmptyNode(StructuredNode node) => EmitsNothing(node);

    private static bool AlwaysTerminates(StructuredNode? node) => node switch
    {
        GotoNode or ReturnNode or BreakNode or ContinueNode => true,
        BlockNode block => block.Kind is TermKind.Return or TermKind.Exit or TermKind.Goto
            || (block.Kind == TermKind.If && AlwaysTerminates(block.Then) && AlwaysTerminates(block.Else)),
        SeqNode seq => seq.Children.Count > 0 && AlwaysTerminates(seq.Children[^1]),
        SwitchNode switchNode => SwitchAlwaysTerminates(switchNode),
        _ => false
    };

    private static bool SwitchAlwaysTerminates(SwitchNode node)
    {
        if (node.Default is null || !AlwaysTerminates(node.Default)) return false;
        foreach (var switchCase in node.Cases)
        {
            if (!AlwaysTerminates(switchCase.Body)) return false;
        }
        return true;
    }

    private void EmitLoop(LoopNode loop)
    {
        Line("while (true)");
        OpenBraces();
        var body = loop.Body;
        if (body is SeqNode seq && seq.Children.Count > 0 && seq.Children[^1] is ContinueNode)
            body = new SeqNode(seq.Children.GetRange(0, seq.Children.Count - 1));
        Emit(body);
        CloseBraces();
    }

    private void EmitBlock(BlockNode node)
    {
        var block = _cfg.Blocks[node.Block];
        EmitLeaves(block, _gotoTargets.Contains(node.Block) ? $"Label_{_cfg.LabelNumber(node.Block)}:" : null);

        switch (node.Kind)
        {
            case TermKind.Return:
                var term = _cfg.Statements[block.End];
                Line(term is EX_Return ? $"{BlueprintDecompilerUtils.GetLineExpression(term)};" : "return;");
                break;
            case TermKind.Goto:
                Line($"goto Label_{_cfg.LabelNumber(node.GotoTarget)};");
                break;
            case TermKind.If:
                EmitIf(ControlFlowGraph.ConditionOf(_cfg.Statements[block.End]), node.Then, node.Else);
                break;
        }
    }

    private void EmitLeaves(BasicBlock block, string? label) => EmitLeaves(block, label, _cfg.LeafEnd(block));

    private void EmitLeaves(BasicBlock block, string? label, int leafEnd)
    {
        for (var i = block.Start; i <= leafEnd; i++)
        {
            var statement = _cfg.Statements[i];
            if (ControlFlowGraph.IsSkipped(statement))
                continue;
            var expression = BlueprintDecompilerUtils.GetLineExpression(statement);
            if (string.IsNullOrWhiteSpace(expression))
                continue;
            if (label != null)
            {
                Line(label);
                _fresh = true;
                label = null;
            }
            Line($"{expression};");
        }
        if (label != null)
            Line(label);
    }

    private void EmitIf(KismetExpression condition, StructuredNode? thenNode, StructuredNode? elseNode)
    {
        var rendered = BlueprintDecompilerUtils.GetLineExpression(condition);
        var thenEmpty = EmitsNothing(thenNode);
        var elseEmpty = EmitsNothing(elseNode);

        if (thenEmpty && !elseEmpty)
        {
            Line($"if (!({rendered}))");
            OpenBraces();
            Emit(elseNode!);
            CloseBraces();
            return;
        }

        Line($"if ({rendered})");
        OpenBraces();
        if (!thenEmpty) Emit(thenNode!);
        CloseBraces();
        if (!elseEmpty)
        {
            _builder.AppendLine("else");
            OpenBraces();
            Emit(elseNode!);
            CloseBraces();
        }
    }

    private bool EmitsNothing(StructuredNode? node)
    {
        switch (node)
        {
            case null:
                return true;
            case SeqNode seq:
                foreach (var child in seq.Children)
                    if (!EmitsNothing(child)) return false;
                return true;
            case BlockNode block:
                if (block.Kind is not (TermKind.FallThrough or TermKind.Exit)) return false;
                if (_gotoTargets.Contains(block.Block)) return false;
                return !HasEmittableLeaf(block.Block);
            default:
                return false;
        }
    }

    private bool HasEmittableLeaf(int blockIndex)
    {
        var block = _cfg.Blocks[blockIndex];
        var leafEnd = _cfg.LeafEnd(block);
        for (var i = block.Start; i <= leafEnd; i++)
        {
            if (ControlFlowGraph.IsSkipped(_cfg.Statements[i]))
                continue;
            if (!string.IsNullOrWhiteSpace(BlueprintDecompilerUtils.GetLineExpression(_cfg.Statements[i])))
                return true;
        }
        return false;
    }

    private void Line(string text)
    {
        if (!_fresh) _builder.AppendLine();
        _builder.AppendLine(text);
        _fresh = false;
    }

    private void OpenBraces()
    {
        _builder.AppendLine("{");
        _builder.IncreaseIndentation();
        _fresh = true;
    }

    private void CloseBraces()
    {
        _builder.DecreaseIndentation();
        _builder.AppendLine("}");
        _fresh = false;
    }
}
