using System.Collections.Generic;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

internal sealed class CfgEquivalence
{
    private readonly ControlFlowGraph _cfg;
    private readonly int[] _emitCount;
    private bool _ok = true;

    private CfgEquivalence(ControlFlowGraph cfg)
    {
        _cfg = cfg;
        _emitCount = new int[cfg.Blocks.Length];
    }

    public static bool Verify(ControlFlowGraph cfg, SeqNode root)
    {
        var verifier = new CfgEquivalence(cfg);
        var entry = verifier.Flow(root, cfg.ExitIndex);
        if (!verifier._ok || entry != cfg.EntryIndex)
            return false;

        var reachable = verifier.Reachable();
        for (var b = 0; b < cfg.ExitIndex; b++)
        {
            var expected = reachable[b] ? 1 : 0;
            if (verifier._emitCount[b] != expected)
                return false;
        }
        return true;
    }

    private bool[] Reachable()
    {
        var reachable = new bool[_cfg.Blocks.Length];
        var queue = new Queue<int>();
        reachable[_cfg.EntryIndex] = true;
        queue.Enqueue(_cfg.EntryIndex);
        while (queue.Count > 0)
        {
            foreach (var succ in _cfg.Blocks[queue.Dequeue()].Successors)
            {
                if (!reachable[succ])
                {
                    reachable[succ] = true;
                    queue.Enqueue(succ);
                }
            }
        }
        return reachable;
    }

    private int Flow(StructuredNode? node, int cont)
    {
        switch (node)
        {
            case null:
                return cont;
            case SeqNode seq:
            {
                var c = cont;
                for (var i = seq.Children.Count - 1; i >= 0; i--)
                    c = Flow(seq.Children[i], c);
                return c;
            }
            case GotoNode jump:
                return jump.Target;
            case ReturnNode:
                return _cfg.ExitIndex;
            case BlockNode block:
            {
                _emitCount[block.Block]++;
                switch (block.Kind)
                {
                    case TermKind.FallThrough:
                        CheckSingle(block.Block, cont);
                        break;
                    case TermKind.Return:
                    case TermKind.Exit:
                        CheckSingle(block.Block, _cfg.ExitIndex);
                        break;
                    case TermKind.Goto:
                        CheckSingle(block.Block, block.GotoTarget);
                        break;
                    case TermKind.If:
                        CheckBranch(block.Block, Flow(block.Then, cont), Flow(block.Else, cont));
                        break;
                }
                return block.Block;
            }
            default:
                _ok = false;
                return cont;
        }
    }

    private void CheckSingle(int block, int target)
    {
        var expected = _cfg.Blocks[block].Successors;
        if (expected.Count != 1 || expected[0] != target)
            _ok = false;
    }

    private void CheckBranch(int block, int thenTarget, int elseTarget)
    {
        var expected = _cfg.Blocks[block].Successors;
        if (expected.Count != 2 || expected[0] != thenTarget || expected[1] != elseTarget)
            _ok = false;
    }
}
