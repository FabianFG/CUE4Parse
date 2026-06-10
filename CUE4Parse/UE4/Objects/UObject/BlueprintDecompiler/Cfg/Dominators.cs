using System;
using System.Collections.Generic;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

internal sealed class Dominators
{
    public readonly int[] Idom;
    public readonly int[] PostIdom;
    public readonly List<(int From, int To)> BackEdges;

    private Dominators(int[] idom, int[] postIdom, List<(int From, int To)> backEdges)
    {
        Idom = idom;
        PostIdom = postIdom;
        BackEdges = backEdges;
    }

    public static Dominators Compute(ControlFlowGraph cfg)
    {
        var blocks = cfg.Blocks;
        var count = blocks.Length;

        var idom = ComputeIdoms(count, cfg.EntryIndex, n => blocks[n].Successors, n => blocks[n].Predecessors);
        var postIdom = ComputeIdoms(count, cfg.ExitIndex, n => blocks[n].Predecessors, n => blocks[n].Successors);

        var backEdges = new List<(int From, int To)>();
        foreach (var block in blocks)
        {
            foreach (var succ in block.Successors)
            {
                if (Dominates(idom, succ, block.Index))
                    backEdges.Add((block.Index, succ));
            }
        }

        return new Dominators(idom, postIdom, backEdges);
    }

    public bool Dominates(int a, int b) => Dominates(Idom, a, b);

    public bool PostDominates(int a, int b) => Dominates(PostIdom, a, b);

    private static bool Dominates(int[] idom, int a, int b)
    {
        if (a == b) return true;
        if (b < 0 || idom[b] < 0) return false;
        var x = b;
        while (idom[x] != x)
        {
            x = idom[x];
            if (x == a) return true;
        }
        return false;
    }

    private static int[] ComputeIdoms(int count, int root, Func<int, IReadOnlyList<int>> successors, Func<int, IReadOnlyList<int>> predecessors)
    {
        var order = new List<int>(count);
        var visited = new bool[count];
        var iter = new int[count];
        var stack = new Stack<int>();
        stack.Push(root);
        visited[root] = true;
        while (stack.Count > 0)
        {
            var n = stack.Peek();
            var succs = successors(n);
            if (iter[n] < succs.Count)
            {
                var c = succs[iter[n]++];
                if (!visited[c])
                {
                    visited[c] = true;
                    stack.Push(c);
                }
            }
            else
            {
                order.Add(n);
                stack.Pop();
            }
        }
        order.Reverse();

        var rpo = new int[count];
        for (var i = 0; i < count; i++) rpo[i] = -1;
        for (var i = 0; i < order.Count; i++) rpo[order[i]] = i;

        var idom = new int[count];
        for (var i = 0; i < count; i++) idom[i] = -1;
        idom[root] = root;

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var b in order)
            {
                if (b == root) continue;
                var newIdom = -1;
                foreach (var p in predecessors(b))
                {
                    if (rpo[p] < 0 || idom[p] < 0) continue;
                    newIdom = newIdom < 0 ? p : Intersect(p, newIdom, idom, rpo);
                }
                if (newIdom >= 0 && idom[b] != newIdom)
                {
                    idom[b] = newIdom;
                    changed = true;
                }
            }
        }
        return idom;
    }

    private static int Intersect(int a, int b, int[] idom, int[] rpo)
    {
        while (a != b)
        {
            while (rpo[a] > rpo[b]) a = idom[a];
            while (rpo[b] > rpo[a]) b = idom[b];
        }
        return a;
    }
}
