using System.Collections.Generic;
using CUE4Parse.UE4.Kismet;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

internal sealed class Structurer
{
    private const int MaxNesting = 12;

    private readonly ControlFlowGraph _cfg;
    private readonly Dominators _dom;
    private readonly bool[] _emitted;
    public readonly HashSet<int> GotoTargets = [];

    private Structurer(ControlFlowGraph cfg, Dominators dom)
    {
        _cfg = cfg;
        _dom = dom;
        _emitted = new bool[cfg.Blocks.Length];
    }

    public static Structurer? Structure(ControlFlowGraph cfg, Dominators dom, out SeqNode? root)
    {
        root = null;
        if (!dom.IsReducible) return null;
        if (dom.BackEdges.Count > 0) return null;
        if (UsesExecutionFlowStack(cfg)) return null;

        var structurer = new Structurer(cfg, dom);
        var built = structurer.Build(cfg.EntryIndex, cfg.ExitIndex, 0);
        if (built is null) return null;

        root = built;
        return structurer;
    }

    private SeqNode? Build(int start, int stop, int depth)
    {
        var children = new List<StructuredNode>();
        var current = start;
        while (current != stop && current != _cfg.ExitIndex)
        {
            if (_emitted[current])
            {
                children.Add(new GotoNode(current));
                GotoTargets.Add(current);
                return new SeqNode(children);
            }

            _emitted[current] = true;
            var block = _cfg.Blocks[current];
            var term = _cfg.Statements[block.End];

            if (term is EX_Return)
            {
                children.Add(new BlockNode(current, TermKind.Return));
                return new SeqNode(children);
            }
            if (term is EX_EndOfScript)
            {
                children.Add(new BlockNode(current, TermKind.Exit));
                return new SeqNode(children);
            }
            if (ControlFlowGraph.IsConditional(term))
            {
                if (block.Successors.Count != 2 || depth >= MaxNesting) return null;
                if (BlueprintDecompilerUtils.GetLineExpression(ControlFlowGraph.ConditionOf(term)).Contains('\n')) return null;
                var follow = _dom.PostIdom[current];
                if (follow < 0) return null;

                var thenBranch = BranchRegion(block.Successors[0], follow, depth + 1);
                var elseBranch = BranchRegion(block.Successors[1], follow, depth + 1);
                if (thenBranch is null || elseBranch is null) return null;

                children.Add(new BlockNode(current, TermKind.If) { Then = thenBranch, Else = elseBranch });
                current = follow;
                continue;
            }

            if (block.Successors.Count != 1) return null;
            var next = block.Successors[0];
            if (next == _cfg.ExitIndex)
            {
                children.Add(new BlockNode(current, term is EX_PopExecutionFlow ? TermKind.Return : TermKind.FallThrough));
                current = next;
            }
            else if (next == stop)
            {
                children.Add(new BlockNode(current, TermKind.FallThrough));
                current = next;
            }
            else if (_emitted[next])
            {
                children.Add(new BlockNode(current, TermKind.Goto) { GotoTarget = next });
                GotoTargets.Add(next);
                return new SeqNode(children);
            }
            else
            {
                children.Add(new BlockNode(current, TermKind.FallThrough));
                current = next;
            }
        }

        return new SeqNode(children);
    }

    private StructuredNode? BranchRegion(int target, int follow, int depth)
    {
        if (target == follow) return new SeqNode([]);
        if (target == _cfg.ExitIndex) return new ReturnNode();
        return Build(target, follow, depth);
    }

    private static bool UsesExecutionFlowStack(ControlFlowGraph cfg)
    {
        foreach (var statement in cfg.Statements)
        {
            if (statement is EX_PushExecutionFlow or EX_PopExecutionFlow or EX_PopExecutionFlowIfNot)
                return true;
        }
        return false;
    }
}
