using System.Collections.Generic;
using CUE4Parse.UE4.Kismet;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

internal sealed class CfgEquivalence
{
    private readonly ControlFlowGraph _cfg;
    private readonly List<int>[] _expected;
    private readonly int[] _emitCount;
    private bool _ok = true;

    private CfgEquivalence(ControlFlowGraph cfg, List<int>[] expected)
    {
        _cfg = cfg;
        _expected = expected;
        _emitCount = new int[cfg.Blocks.Length];
    }

    public static bool Verify(ControlFlowGraph cfg, SeqNode root)
    {
        var expected = DeriveEdges(cfg);
        if (expected is null)
            return false;

        var verifier = new CfgEquivalence(cfg, expected);
        var entry = verifier.Flow(root, cfg.ExitIndex, null);
        if (!verifier._ok || entry != cfg.EntryIndex)
            return false;

        var reachable = verifier.Reachable();
        for (var b = 0; b < cfg.ExitIndex; b++)
        {
            if (verifier._emitCount[b] != (reachable[b] ? 1 : 0))
                return false;
        }
        return true;
    }

    private static List<int>[]? DeriveEdges(ControlFlowGraph cfg)
    {
        var code = cfg.Statements;
        var exit = cfg.ExitIndex;

        var offsetToIndex = new Dictionary<int, int>(code.Length);
        for (var i = 0; i < code.Length; i++)
            offsetToIndex[code[i].StatementIndex] = i;

        var indexToBlock = new int[code.Length];
        for (var b = 0; b < exit; b++)
            for (var i = cfg.Blocks[b].Start; i <= cfg.Blocks[b].End; i++)
                indexToBlock[i] = b;

        var popTarget = new Dictionary<int, int>();
        var simStack = new Stack<int>();
        for (var i = 0; i < code.Length; i++)
        {
            switch (code[i])
            {
                case EX_PushExecutionFlow push:
                    simStack.Push((int) push.PushingAddress);
                    break;
                case EX_PopExecutionFlow:
                case EX_PopExecutionFlowIfNot:
                    if (simStack.Count == 0) popTarget[i] = exit;
                    else if (!offsetToIndex.TryGetValue(simStack.Pop(), out var popIndex)) return null;
                    else popTarget[i] = indexToBlock[popIndex];
                    break;
            }
        }

        var expected = new List<int>[cfg.Blocks.Length];
        for (var b = 0; b < exit; b++)
        {
            var block = cfg.Blocks[b];
            var list = new List<int>(2);
            switch (code[block.End])
            {
                case EX_JumpIfNot jumpIfNot:
                    if (!offsetToIndex.TryGetValue((int) jumpIfNot.CodeOffset, out var jumpIfNotTarget)) return null;
                    list.Add(b + 1);
                    list.Add(indexToBlock[jumpIfNotTarget]);
                    break;
                case EX_Skip:
                    list.Add(b + 1);
                    break;
                case EX_Jump jump:
                    if (!offsetToIndex.TryGetValue((int) jump.CodeOffset, out var jumpTarget)) return null;
                    list.Add(indexToBlock[jumpTarget]);
                    break;
                case EX_Return:
                case EX_EndOfScript:
                    list.Add(exit);
                    break;
                case EX_PopExecutionFlow:
                    list.Add(popTarget[block.End]);
                    break;
                case EX_PopExecutionFlowIfNot:
                    list.Add(b + 1);
                    list.Add(popTarget[block.End]);
                    break;
                case EX_ComputedJump:
                    return null;
                default:
                    list.Add(b + 1);
                    break;
            }
            expected[b] = list;
        }
        expected[exit] = [];
        return expected;
    }

    private bool[] Reachable()
    {
        var reachable = new bool[_cfg.Blocks.Length];
        var queue = new Queue<int>();
        reachable[_cfg.EntryIndex] = true;
        queue.Enqueue(_cfg.EntryIndex);
        while (queue.Count > 0)
        {
            foreach (var succ in _expected[queue.Dequeue()])
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

    private int Flow(StructuredNode? node, int cont, (int Header, int Follow)? loop)
    {
        switch (node)
        {
            case null:
                return cont;
            case SeqNode seq:
            {
                var c = cont;
                for (var i = seq.Children.Count - 1; i >= 0; i--)
                    c = Flow(seq.Children[i], c, loop);
                return c;
            }
            case GotoNode jump:
                return jump.Target;
            case ReturnNode:
                return _cfg.ExitIndex;
            case BreakNode:
                if (loop is null) { _ok = false; return cont; }
                return loop.Value.Follow;
            case ContinueNode:
                if (loop is null) { _ok = false; return cont; }
                return loop.Value.Header;
            case LoopNode loopNode:
                if (Flow(loopNode.Body, loopNode.Header, (loopNode.Header, cont)) != loopNode.Header)
                    _ok = false;
                return loopNode.Header;
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
                        CheckBranch(block.Block, Flow(block.Then, cont, loop), Flow(block.Else, cont, loop));
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
        var expected = _expected[block];
        if (expected.Count != 1 || expected[0] != target)
            _ok = false;
    }

    private void CheckBranch(int block, int thenTarget, int elseTarget)
    {
        var expected = _expected[block];
        if (expected.Count != 2 || expected[0] != thenTarget || expected[1] != elseTarget)
            _ok = false;
    }
}
