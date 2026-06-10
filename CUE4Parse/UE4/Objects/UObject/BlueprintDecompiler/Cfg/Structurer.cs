using System.Collections.Generic;
using CUE4Parse.UE4.Kismet;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

internal sealed class Structurer
{
    private const int MaxNesting = 12;

    private enum Transfer
    {
        Normal,
        Continue,
        Break,
        Refuse
    }

    private sealed class LoopFrame
    {
        public readonly int Header;
        public readonly int Follow;
        public readonly LoopFrame? Outer;

        public LoopFrame(int header, int follow, LoopFrame? outer)
        {
            Header = header;
            Follow = follow;
            Outer = outer;
        }
    }

    private readonly ControlFlowGraph _cfg;
    private readonly Dominators _dom;
    private readonly LoopInfo _loops;
    private readonly bool[] _emitted;
    public readonly HashSet<int> GotoTargets = [];

    private Structurer(ControlFlowGraph cfg, Dominators dom, LoopInfo loops)
    {
        _cfg = cfg;
        _dom = dom;
        _loops = loops;
        _emitted = new bool[cfg.Blocks.Length];
    }

    public static Structurer? Structure(ControlFlowGraph cfg, Dominators dom, out SeqNode? root)
    {
        root = null;
        if (!dom.IsReducible) return null;

        var loops = LoopInfo.Compute(cfg, dom);
        if (loops is null) return null;

        var structurer = new Structurer(cfg, dom, loops);
        var built = structurer.Build(cfg.EntryIndex, cfg.ExitIndex, 0, null);
        if (built is null) return null;

        root = built;
        return structurer;
    }

    private SeqNode? Build(int start, int stop, int depth, LoopFrame? loop)
    {
        var children = new List<StructuredNode>();
        var current = start;
        while (current != stop && current != _cfg.ExitIndex)
        {
            if (_loops.IsHeader(current) && !_emitted[current] && (loop is null || loop.Header != current))
            {
                if (depth >= MaxNesting) return null;
                var loopFollow = _loops.FollowOf(current);
                var body = Build(current, _cfg.ExitIndex, depth + 1, new LoopFrame(current, loopFollow, loop));
                if (body is null) return null;
                children.Add(new LoopNode(current, body));
                var loopAdvance = Advance(children, ref current, loopFollow, stop, loop);
                if (loopAdvance is null) return null;
                if (loopAdvance == false) return new SeqNode(children);
                continue;
            }

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

                var thenBranch = BranchRegion(block.Successors[0], follow, depth + 1, loop);
                var elseBranch = BranchRegion(block.Successors[1], follow, depth + 1, loop);
                if (thenBranch is null || elseBranch is null) return null;

                children.Add(new BlockNode(current, TermKind.If) { Then = thenBranch, Else = elseBranch });
                var ifAdvance = Advance(children, ref current, follow, stop, loop);
                if (ifAdvance is null) return null;
                if (ifAdvance == false) return new SeqNode(children);
                continue;
            }

            if (block.Successors.Count != 1) return null;
            var next = block.Successors[0];
            switch (ResolveTransfer(next, loop))
            {
                case Transfer.Refuse:
                    return null;
                case Transfer.Continue:
                    children.Add(new BlockNode(current, TermKind.FallThrough));
                    children.Add(new ContinueNode());
                    return new SeqNode(children);
                case Transfer.Break:
                    children.Add(new BlockNode(current, TermKind.FallThrough));
                    children.Add(new BreakNode());
                    return new SeqNode(children);
            }

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

    private bool? Advance(List<StructuredNode> children, ref int current, int target, int stop, LoopFrame? loop)
    {
        switch (ResolveTransfer(target, loop))
        {
            case Transfer.Refuse:
                return null;
            case Transfer.Continue:
                children.Add(new ContinueNode());
                return false;
            case Transfer.Break:
                children.Add(new BreakNode());
                return false;
        }

        if (target == _cfg.ExitIndex || target == stop)
        {
            current = target;
            return true;
        }
        if (_emitted[target])
        {
            children.Add(new GotoNode(target));
            GotoTargets.Add(target);
            return false;
        }
        current = target;
        return true;
    }

    private StructuredNode? BranchRegion(int target, int ifFollow, int depth, LoopFrame? loop)
    {
        if (target == ifFollow) return new SeqNode([]);
        switch (ResolveTransfer(target, loop))
        {
            case Transfer.Continue: return new ContinueNode();
            case Transfer.Break: return new BreakNode();
            case Transfer.Refuse: return null;
        }
        if (target == _cfg.ExitIndex) return new ReturnNode();
        return Build(target, ifFollow, depth, loop);
    }

    private static Transfer ResolveTransfer(int target, LoopFrame? loop)
    {
        var innermost = true;
        for (var frame = loop; frame is not null; frame = frame.Outer)
        {
            if (target == frame.Header) return innermost ? Transfer.Continue : Transfer.Refuse;
            if (target == frame.Follow) return innermost ? Transfer.Break : Transfer.Refuse;
            innermost = false;
        }
        return Transfer.Normal;
    }
}
