using System.Collections.Generic;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Kismet;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

internal sealed class FoldPlan
{
    public readonly HashSet<int> DroppedDefs = [];
    public readonly Dictionary<int, KismetExpression> Inlined = [];

    public static FoldPlan Compute(ControlFlowGraph cfg)
    {
        var plan = new FoldPlan();
        var code = cfg.Statements;

        foreach (var statement in code)
        {
            if (statement is EX_AutoRtfmTransact or EX_AutoRtfmStopTransact or EX_AutoRtfmAbortIfNot)
                return plan;
        }

        var text = new System.Text.StringBuilder();
        foreach (var block in cfg.Blocks)
        {
            if (block.IsExit) continue;
            for (var i = block.Start; i <= block.End; i++)
            {
                if (ControlFlowGraph.IsSkipped(code[i])) continue;
                text.Append(BlueprintDecompilerUtils.GetLineExpression(code[i]));
                text.Append('\n');
            }
        }
        var rendered = text.ToString();

        foreach (var block in cfg.Blocks)
        {
            if (block.IsExit) continue;
            var leafEnd = cfg.LeafEnd(block);
            var previous = -1;
            for (var i = block.Start; i <= leafEnd; i++)
            {
                if (ControlFlowGraph.IsSkipped(code[i])) continue;
                if (previous >= 0)
                    TryFold(plan, code, previous, i, rendered);
                previous = i;
            }
        }
        return plan;
    }

    private static void TryFold(FoldPlan plan, KismetExpression[] code, int defIndex, int useIndex, string rendered)
    {
        var (definedVariable, definedValue) = AsAssignment(code[defIndex]);
        var (_, usedValue) = AsAssignment(code[useIndex]);
        if (definedVariable is not EX_LocalVariable || definedValue is null) return;
        if (usedValue is not EX_LocalVariable) return;

        var name = BlueprintDecompilerUtils.GetLineExpression(definedVariable);
        if (!IsIdentifier(name)) return;
        if (BlueprintDecompilerUtils.GetLineExpression(usedValue) != name) return;
        if (Regex.Matches(rendered, $@"\b{Regex.Escape(name)}\b").Count != 2) return;
        if (plan.Inlined.ContainsKey(defIndex) || plan.DroppedDefs.Contains(useIndex)) return;

        plan.DroppedDefs.Add(defIndex);
        plan.Inlined[useIndex] = definedValue;
    }

    private static (KismetExpression? Variable, KismetExpression? Value) AsAssignment(KismetExpression statement) => statement switch
    {
        EX_Let let => (let.Variable, let.Assignment),
        EX_LetBase letBase => (letBase.Variable, letBase.Assignment),
        _ => (null, null)
    };

    private static bool IsIdentifier(string name)
    {
        if (name.Length == 0) return false;
        foreach (var c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != '_') return false;
        }
        return true;
    }
}
