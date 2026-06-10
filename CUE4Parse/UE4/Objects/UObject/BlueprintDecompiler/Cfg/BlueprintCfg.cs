using System.Collections.Generic;

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
                return false;

            var dom = Dominators.Compute(cfg);
            var structurer = Structurer.Structure(cfg, dom, out var root);
            if (structurer is null || root is null)
                return false;

            if (!CfgEquivalence.Verify(cfg, root))
                return false;

            var folded = SwitchFold.Fold(root, cfg);
            if (SwitchFold.Depth(folded) > MaxOutputNesting)
                return false;

            new StructuredEmitter(cfg, structurer.GotoTargets, FoldPlan.Compute(cfg), builder).Emit(folded);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
