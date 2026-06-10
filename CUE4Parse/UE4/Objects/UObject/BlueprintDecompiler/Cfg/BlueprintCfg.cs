using System.Collections.Generic;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

public static class BlueprintCfg
{
    public static bool TryStructure(UFunction function, List<int> jumpTargets, out string body)
    {
        body = string.Empty;

        try
        {
            var cfg = ControlFlowGraph.Build(function);
            if (cfg is null)
                return false;

            var dom = Dominators.Compute(cfg);
            if (!dom.IsReducible)
                return false;
        }
        catch
        {
            return false;
        }

        return false;
    }
}
