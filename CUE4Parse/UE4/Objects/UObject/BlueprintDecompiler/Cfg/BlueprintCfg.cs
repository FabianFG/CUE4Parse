using System.Collections.Generic;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

public static class BlueprintCfg
{
    public static bool TryStructure(UFunction function, List<int> jumpTargets, CustomStringBuilder builder)
    {
        try
        {
            var cfg = ControlFlowGraph.Build(function);
            if (cfg is null)
                return false;

            var dom = Dominators.Compute(cfg);
            var structurer = Structurer.Structure(cfg, dom, out var root);
            if (structurer is null || root is null)
                return false;

            if (!CfgEquivalence.Verify(cfg, root))
                return false;

            new StructuredEmitter(cfg, structurer.GotoTargets, builder).Emit(root);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
