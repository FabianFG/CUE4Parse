using System.Collections.Generic;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

public sealed class UbergraphInlinePlan
{
    private readonly ControlFlowGraph _cfg;
    private readonly HashSet<int> _gotoTargets;
    private readonly FoldPlan _fold;
    private readonly Dictionary<int, Region> _regions;

    private readonly record struct Region(StructuredNode Node, int EntryBlock, bool KeepLabel);

    private UbergraphInlinePlan(ControlFlowGraph cfg, HashSet<int> gotoTargets, FoldPlan fold, Dictionary<int, Region> regions)
    {
        _cfg = cfg;
        _gotoTargets = gotoTargets;
        _fold = fold;
        _regions = regions;
    }

    public IReadOnlyCollection<int> EntryOffsets => _regions.Keys;

    public bool Contains(int entryOffset) => _regions.ContainsKey(entryOffset);

    public bool TryEmit(int entryOffset, CustomStringBuilder builder)
    {
        if (!_regions.TryGetValue(entryOffset, out var region))
            return false;

        var targets = _gotoTargets;
        if (!region.KeepLabel && _gotoTargets.Contains(region.EntryBlock))
        {
            targets = new HashSet<int>(_gotoTargets);
            targets.Remove(region.EntryBlock);
        }

        new StructuredEmitter(_cfg, targets, _fold, builder).Emit(region.Node);
        return true;
    }

    internal static UbergraphInlinePlan? TryCreate(UFunction function, List<int> entryOffsets)
    {
        try
        {
            var cfg = ControlFlowGraph.Build(function, entryOffsets);
            if (cfg is null)
                return null;

            var dom = Dominators.Compute(cfg);
            var structurer = Structurer.Structure(cfg, dom, out var root);
            if (structurer is null || root is null)
                return null;
            if (!CfgEquivalence.Verify(cfg, root))
                return null;

            if (root.Children.Count == 0 || root.Children[0] is not ComputedGotoNode dispatch)
                return null;
            if (root.Children.Count - 1 != dispatch.Entries.Count)
                return null;

            var dispatchBlock = cfg.Blocks[dispatch.Block];
            for (var i = dispatchBlock.Start; i <= cfg.LeafEnd(dispatchBlock); i++)
            {
                if (!ControlFlowGraph.IsSkipped(cfg.Statements[i]))
                    return null;
            }

            if (!Partitions(cfg, dispatch))
                return null;

            var fold = FoldPlan.Compute(cfg);
            var regions = new Dictionary<int, Region>(dispatch.Entries.Count);
            for (var k = 0; k < dispatch.Entries.Count; k++)
            {
                var entryBlock = dispatch.Entries[k];
                var offset = cfg.LabelNumber(entryBlock);
                var keepLabel = false;
                foreach (var pred in cfg.Blocks[entryBlock].Predecessors)
                {
                    if (pred != dispatch.Block) { keepLabel = true; break; }
                }
                if (!regions.TryAdd(offset, new Region(root.Children[k + 1], entryBlock, keepLabel)))
                    return null;
            }

            return new UbergraphInlinePlan(cfg, structurer.GotoTargets, fold, regions);
        }
        catch
        {
            return null;
        }
    }

    private static bool Partitions(ControlFlowGraph cfg, ComputedGotoNode dispatch)
    {
        var owner = new int[cfg.Blocks.Length];
        for (var i = 0; i < owner.Length; i++)
            owner[i] = -1;

        var work = new Stack<int>();
        for (var entry = 0; entry < dispatch.Entries.Count; entry++)
        {
            work.Push(dispatch.Entries[entry]);
            while (work.Count > 0)
            {
                var block = work.Pop();
                if (block == dispatch.Block || block == cfg.ExitIndex)
                    return false;
                if (owner[block] == entry)
                    continue;
                if (owner[block] != -1)
                    return false;

                owner[block] = entry;
                foreach (var successor in cfg.Blocks[block].Successors)
                {
                    if (successor == cfg.ExitIndex)
                        continue;
                    work.Push(successor);
                }
            }
        }

        for (var block = 0; block < cfg.ExitIndex; block++)
        {
            if (block != dispatch.Block && owner[block] == -1 && !ControlFlowGraph.IsTriviallyEmpty(cfg.Statements, cfg.Blocks[block]))
                return false;
        }

        return true;
    }
}
