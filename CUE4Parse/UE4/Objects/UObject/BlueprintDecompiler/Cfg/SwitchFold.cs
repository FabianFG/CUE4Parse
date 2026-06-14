using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

internal static class SwitchFold
{
    public static int Depth(StructuredNode? node)
    {
        switch (node)
        {
            case SeqNode seq:
            {
                var max = 0;
                foreach (var child in seq.Children) max = Math.Max(max, Depth(child));
                return max;
            }
            case BlockNode { Kind: TermKind.If } block:
                return 1 + Math.Max(Depth(block.Then), Depth(block.Else));
            case LoopNode loop:
                return 1 + Depth(loop.Body);
            case SwitchNode switchNode:
            {
                var max = Depth(switchNode.Default);
                foreach (var switchCase in switchNode.Cases) max = Math.Max(max, Depth(switchCase.Body));
                return 1 + max;
            }
            default:
                return 0;
        }
    }

    private sealed class Link
    {
        public string SubjectText = string.Empty;
        public string ConstantText = string.Empty;
        public string? TempText;
        public int Block;
        public int PrefixLeafEnd;
        public bool HasPrefix;
        public StructuredNode CaseBranch = null!;
        public StructuredNode ContinuationBranch = null!;
    }

    public static StructuredNode Fold(StructuredNode node, ControlFlowGraph cfg)
    {
        switch (node)
        {
            case SeqNode seq:
                return new SeqNode(seq.Children.ConvertAll(child => Fold(child, cfg)));
            case LoopNode loop:
                return new LoopNode(loop.Header, Fold(loop.Body, cfg));
            case BlockNode { Kind: TermKind.If } block:
                var folded = TryBuildSwitch(block, cfg);
                if (folded is not null) return folded;
                block.Then = block.Then is null ? null : Fold(block.Then, cfg);
                block.Else = block.Else is null ? null : Fold(block.Else, cfg);
                return block;
            default:
                return node;
        }
    }

    private static StructuredNode? TryBuildSwitch(BlockNode head, ControlFlowGraph cfg)
    {
        var cases = new List<SwitchCase>();
        var seenConstants = new HashSet<string>();
        var chainBlocks = new HashSet<int>();
        var temps = new List<string>();
        string? subjectText = null;
        var prefixBlock = -1;
        var prefixLeafEnd = -1;

        StructuredNode current = head;
        var first = true;
        while (true)
        {
            var node = AsLink(current);
            if (node is null) break;
            var link = Describe(node, cfg);
            if (link is null) break;
            if (!first && link.HasPrefix) break;
            if (subjectText is null) subjectText = link.SubjectText;
            else if (link.SubjectText != subjectText) break;
            if (!seenConstants.Add(link.ConstantText)) break;

            if (first && link.HasPrefix)
            {
                prefixBlock = link.Block;
                prefixLeafEnd = link.PrefixLeafEnd;
            }
            cases.Add(new SwitchCase(link.ConstantText, Fold(link.CaseBranch, cfg)));
            chainBlocks.Add(link.Block);
            if (link.TempText is not null) temps.Add(link.TempText);
            current = link.ContinuationBranch;
            first = false;
        }

        if (cases.Count < 2 || subjectText is null || subjectText.Contains('\n'))
            return null;
        if (!TempsUnusedOutside(temps, chainBlocks, cfg))
            return null;

        var switchNode = new SwitchNode(subjectText, cases, Fold(current, cfg));
        if (prefixBlock < 0 || prefixLeafEnd < cfg.Blocks[prefixBlock].Start)
            return switchNode;
        return new SeqNode([new LeafRangeNode(prefixBlock, prefixLeafEnd), switchNode]);
    }

    private static BlockNode? AsLink(StructuredNode node) => node switch
    {
        BlockNode { Kind: TermKind.If } block => block,
        SeqNode { Children: [BlockNode { Kind: TermKind.If } only] } => only,
        _ => null
    };

    private static Link? Describe(BlockNode node, ControlFlowGraph cfg)
    {
        var block = cfg.Blocks[node.Block];
        if (cfg.Statements[block.End] is not EX_JumpIfNot jumpIfNot)
            return null;

        var leafEnd = cfg.LeafEnd(block);
        var nonSkipped = new List<int>();
        for (var i = block.Start; i <= leafEnd; i++)
        {
            if (!ControlFlowGraph.IsSkipped(cfg.Statements[i]))
                nonSkipped.Add(i);
        }

        EX_FinalFunction comparison;
        string? tempText;
        int prefixLeafEnd;
        bool hasPrefix;

        if (jumpIfNot.BooleanExpression is EX_VariableBase conditionVariable)
        {
            if (nonSkipped.Count == 0) return null;
            var letIndex = nonSkipped[^1];
            var (variable, assignment) = cfg.Statements[letIndex] switch
            {
                EX_Let let => (let.Variable, let.Assignment),
                EX_LetBase letBase => (letBase.Variable, letBase.Assignment),
                _ => ((KismetExpression?) null, (KismetExpression?) null)
            };
            if (variable is null || assignment is not EX_FinalFunction call) return null;
            tempText = BlueprintDecompilerUtils.GetLineExpression(conditionVariable);
            if (BlueprintDecompilerUtils.GetLineExpression(variable) != tempText) return null;
            comparison = call;
            prefixLeafEnd = letIndex - 1;
            hasPrefix = nonSkipped.Count > 1;
        }
        else if (jumpIfNot.BooleanExpression is EX_FinalFunction inline)
        {
            comparison = inline;
            tempText = null;
            prefixLeafEnd = leafEnd;
            hasPrefix = nonSkipped.Count > 0;
        }
        else
        {
            return null;
        }

        var functionName = comparison.StackNode.ToString().SubstringAfter(':').Trim('\'');
        bool isEqual;
        if (functionName.StartsWith("EqualEqual_")) isEqual = true;
        else if (functionName.StartsWith("NotEqual_") && !functionName.StartsWith("NotEqualExactly_")) isEqual = false;
        else return null;

        if (comparison.Parameters.Length != 2) return null;
        var subject = comparison.Parameters[0];
        var constant = comparison.Parameters[1];
        if (!IsLiteral(constant) || !IsPure(subject)) return null;

        return new Link
        {
            SubjectText = BlueprintDecompilerUtils.GetLineExpression(subject),
            ConstantText = BlueprintDecompilerUtils.GetLineExpression(constant),
            TempText = tempText,
            Block = node.Block,
            PrefixLeafEnd = prefixLeafEnd,
            HasPrefix = hasPrefix,
            CaseBranch = (isEqual ? node.Then : node.Else) ?? new SeqNode([]),
            ContinuationBranch = (isEqual ? node.Else : node.Then) ?? new SeqNode([])
        };
    }

    private static bool TempsUnusedOutside(List<string> temps, HashSet<int> chainBlocks, ControlFlowGraph cfg)
    {
        if (temps.Count == 0) return true;
        for (var b = 0; b < cfg.ExitIndex; b++)
        {
            if (chainBlocks.Contains(b)) continue;
            var block = cfg.Blocks[b];
            for (var i = block.Start; i <= block.End; i++)
            {
                if (ControlFlowGraph.IsSkipped(cfg.Statements[i])) continue;
                var rendered = BlueprintDecompilerUtils.GetLineExpression(cfg.Statements[i]);
                foreach (var temp in temps)
                {
                    if (rendered.Contains(temp)) return false;
                }
            }
        }
        return true;
    }

    private static bool IsLiteral(KismetExpression expr) => expr is
        EX_IntConst or EX_IntConstByte or EX_ByteConst or EX_Int64Const or EX_UInt64Const or
        EX_NameConst or EX_StringConst or EX_UnicodeStringConst or
        EX_True or EX_False or EX_IntZero or EX_IntOne;

    private static bool IsPure(KismetExpression expr) => expr switch
    {
        EX_Context_FailSilent => false,
        EX_VariableBase => true,
        EX_Self => true,
        EX_Context context => IsPure(context.ObjectExpression) && IsPure(context.ContextExpression),
        EX_StructMemberContext structMember => IsPure(structMember.StructExpression),
        EX_InterfaceContext interfaceContext => IsPure(interfaceContext.InterfaceValue),
        EX_ArrayGetByRef arrayRef => IsPure(arrayRef.ArrayVariable) && IsPure(arrayRef.ArrayIndex),
        EX_NoObject or EX_NoInterface => true,
        _ => IsLiteral(expr)
    };
}
