using System.Collections.Generic;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

public static class BlueprintCfg
{
    public static bool TryStructure(UFunction function, List<int> jumpTargets, out string body)
    {
        body = string.Empty;

        ControlFlowGraph? cfg;
        try
        {
            cfg = ControlFlowGraph.Build(function);
        }
        catch
        {
            return false;
        }

        if (cfg is null)
            return false;

        return false;
    }
}
