using System.Collections.Generic;
using CUE4Parse.UE4.Kismet;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

internal sealed class ControlFlowGraph
{
    public readonly KismetExpression[] Statements;
    public readonly BasicBlock[] Blocks;
    public readonly int EntryIndex;
    public readonly int ExitIndex;

    private ControlFlowGraph(KismetExpression[] statements, BasicBlock[] blocks, int entryIndex, int exitIndex)
    {
        Statements = statements;
        Blocks = blocks;
        EntryIndex = entryIndex;
        ExitIndex = exitIndex;
    }

    public static ControlFlowGraph? Build(UFunction function)
    {
        var code = function.ScriptBytecode;
        if (code == null || code.Length == 0)
            return null;

        var offsetToIndex = new Dictionary<int, int>(code.Length);
        for (var i = 0; i < code.Length; i++)
            offsetToIndex[code[i].StatementIndex] = i;

        var leaders = new SortedSet<int> { 0 };
        for (var i = 0; i < code.Length; i++)
        {
            switch (code[i])
            {
                case EX_ComputedJump:
                    return null;
                case EX_JumpIfNot jumpIfNot:
                    if (!offsetToIndex.TryGetValue((int) jumpIfNot.CodeOffset, out var jumpIfNotTarget)) return null;
                    leaders.Add(jumpIfNotTarget);
                    if (i + 1 < code.Length) leaders.Add(i + 1);
                    break;
                case EX_Skip:
                    break;
                case EX_Jump jump:
                    if (!offsetToIndex.TryGetValue((int) jump.CodeOffset, out var jumpTarget)) return null;
                    leaders.Add(jumpTarget);
                    if (i + 1 < code.Length) leaders.Add(i + 1);
                    break;
                case EX_Return:
                case EX_EndOfScript:
                    if (i + 1 < code.Length) leaders.Add(i + 1);
                    break;
                case EX_PushExecutionFlow push:
                    if (!offsetToIndex.TryGetValue((int) push.PushingAddress, out var pushTarget)) return null;
                    leaders.Add(pushTarget);
                    break;
                case EX_PopExecutionFlow:
                case EX_PopExecutionFlowIfNot:
                    if (i + 1 < code.Length) leaders.Add(i + 1);
                    break;
            }
        }

        var leaderList = new List<int>(leaders);
        var indexToBlock = new int[code.Length];
        var blockList = new List<BasicBlock>(leaderList.Count + 1);
        for (var b = 0; b < leaderList.Count; b++)
        {
            var start = leaderList[b];
            var end = (b + 1 < leaderList.Count ? leaderList[b + 1] : code.Length) - 1;
            blockList.Add(new BasicBlock(b, start, end));
            for (var i = start; i <= end; i++)
                indexToBlock[i] = b;
        }

        var exitIndex = blockList.Count;
        blockList.Add(new BasicBlock(exitIndex, -1, -1));
        var blocks = blockList.ToArray();

        var entryStacks = new FlowStack?[blocks.Length];
        var visited = new bool[blocks.Length];
        var queue = new Queue<int>();
        visited[0] = true;
        queue.Enqueue(0);

        while (queue.Count > 0)
        {
            var bi = queue.Dequeue();
            if (bi == exitIndex)
                continue;

            var block = blocks[bi];
            var stack = entryStacks[bi];
            for (var i = block.Start; i <= block.End; i++)
            {
                if (code[i] is EX_PushExecutionFlow push)
                    stack = new FlowStack((int) push.PushingAddress, stack);
            }

            var successors = new List<(int Block, FlowStack? Stack)>(2);
            switch (code[block.End])
            {
                case EX_JumpIfNot jumpIfNot:
                    successors.Add((bi + 1, stack));
                    successors.Add((indexToBlock[offsetToIndex[(int) jumpIfNot.CodeOffset]], stack));
                    break;
                case EX_Skip:
                    successors.Add((bi + 1, stack));
                    break;
                case EX_Jump jump:
                    successors.Add((indexToBlock[offsetToIndex[(int) jump.CodeOffset]], stack));
                    break;
                case EX_Return:
                case EX_EndOfScript:
                    successors.Add((exitIndex, stack));
                    break;
                case EX_PopExecutionFlow:
                    if (stack is null) successors.Add((exitIndex, null));
                    else successors.Add((indexToBlock[offsetToIndex[stack.Value]], stack.Tail));
                    break;
                case EX_PopExecutionFlowIfNot:
                    successors.Add((bi + 1, stack));
                    if (stack is null) successors.Add((exitIndex, null));
                    else successors.Add((indexToBlock[offsetToIndex[stack.Value]], stack.Tail));
                    break;
                default:
                    successors.Add((bi + 1, stack));
                    break;
            }

            foreach (var (succ, succStack) in successors)
            {
                if (!block.Successors.Contains(succ))
                {
                    block.Successors.Add(succ);
                    blocks[succ].Predecessors.Add(bi);
                }

                if (!visited[succ])
                {
                    visited[succ] = true;
                    entryStacks[succ] = succStack;
                    queue.Enqueue(succ);
                }
                else if (!FlowStack.Equal(entryStacks[succ], succStack))
                {
                    return null;
                }
            }
        }

        for (var b = 0; b < exitIndex; b++)
        {
            if (!visited[b] && !IsTriviallyEmpty(code, blocks[b]))
                return null;
        }

        return new ControlFlowGraph(code, blocks, 0, exitIndex);
    }

    public int LabelNumber(int block) => Statements[Blocks[block].Start].StatementIndex;

    public int LeafEnd(BasicBlock block) => IsControlTerminator(Statements[block.End]) ? block.End - 1 : block.End;

    public static bool IsTriviallyEmpty(KismetExpression[] code, BasicBlock block)
    {
        for (var i = block.Start; i <= block.End; i++)
        {
            if (!IsSkipped(code[i]))
                return false;
        }
        return true;
    }

    public static bool IsSkipped(KismetExpression s) =>
        s is EX_Nothing or EX_NothingInt32 or EX_EndFunctionParms or EX_EndStructConst or EX_EndArray or EX_EndArrayConst or EX_EndSet or EX_EndMap or EX_EndMapConst or EX_EndSetConst or EX_EndOfScript;

    public static bool IsControlTerminator(KismetExpression s) => s switch
    {
        EX_JumpIfNot => true,
        EX_Skip => false,
        EX_Jump => true,
        EX_Return => true,
        EX_EndOfScript => true,
        EX_PopExecutionFlow => true,
        EX_PopExecutionFlowIfNot => true,
        _ => false
    };

    public static bool IsConditional(KismetExpression s) => s is EX_JumpIfNot or EX_PopExecutionFlowIfNot;

    public static KismetExpression ConditionOf(KismetExpression s) => s switch
    {
        EX_JumpIfNot jumpIfNot => jumpIfNot.BooleanExpression,
        EX_PopExecutionFlowIfNot popIfNot => popIfNot.BooleanExpression,
        _ => s
    };

    private sealed class FlowStack
    {
        public readonly int Value;
        public readonly FlowStack? Tail;

        public FlowStack(int value, FlowStack? tail)
        {
            Value = value;
            Tail = tail;
        }

        public static bool Equal(FlowStack? a, FlowStack? b)
        {
            while (a is not null && b is not null)
            {
                if (a.Value != b.Value) return false;
                a = a.Tail;
                b = b.Tail;
            }

            return a is null && b is null;
        }
    }
}
