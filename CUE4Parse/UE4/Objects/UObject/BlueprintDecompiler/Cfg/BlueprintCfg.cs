using System;
using System.Collections.Generic;
using Serilog;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

public static class BlueprintCfg
{
    private const int MaxOutputNesting = 12;

    public static bool TryStructure(UFunction function, List<int> entryOffsets, CustomStringBuilder builder)
    {
        try
        {
            var cfg = ControlFlowGraph.Build(function, entryOffsets);
            if (cfg is null)
            {
                Log.Debug("Structuring fell back to goto for {Function}: control-flow graph could not be built (irreducible or computed jump)", function.Name);
                return false;
            }

            var dom = Dominators.Compute(cfg);
            var structurer = Structurer.Structure(cfg, dom, out var root);
            if (structurer is null || root is null)
            {
                Log.Debug("Structuring fell back to goto for {Function}: structurer refused the region", function.Name);
                return false;
            }

            if (!CfgEquivalence.Verify(cfg, root))
            {
                Log.Debug("Structuring fell back to goto for {Function}: structured output failed the equivalence check", function.Name);
                return false;
            }

            var folded = SwitchFold.Fold(root, cfg);
            if (SwitchFold.Depth(folded) > MaxOutputNesting)
            {
                Log.Debug("Structuring fell back to goto for {Function}: output nesting exceeded {Max}", function.Name, MaxOutputNesting);
                return false;
            }

            new StructuredEmitter(cfg, structurer.GotoTargets, FoldPlan.Compute(cfg), builder).Emit(folded);
            return true;
        }
        catch (Exception e)
        {
            Log.Debug(e, "Structuring threw for {Function}; falling back to goto", function.Name);
            return false;
        }
    }
}
