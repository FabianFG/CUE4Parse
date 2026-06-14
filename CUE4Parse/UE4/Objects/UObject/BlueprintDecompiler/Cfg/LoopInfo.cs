using System.Collections.Generic;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

internal sealed class LoopInfo
{
    private readonly HashSet<int> _headers;
    private readonly Dictionary<int, int> _follow;

    private LoopInfo(HashSet<int> headers, Dictionary<int, int> follow)
    {
        _headers = headers;
        _follow = follow;
    }

    public bool IsHeader(int block) => _headers.Contains(block);

    public int FollowOf(int header) => _follow[header];

    public static LoopInfo? Compute(ControlFlowGraph cfg, Dominators dom)
    {
        var headers = new HashSet<int>();
        var bodies = new Dictionary<int, HashSet<int>>();

        foreach (var (tail, head) in dom.BackEdges)
        {
            headers.Add(head);
            if (!bodies.TryGetValue(head, out var body))
                bodies[head] = body = [head];

            var work = new Stack<int>();
            if (body.Add(tail))
                work.Push(tail);
            while (work.Count > 0)
            {
                foreach (var pred in cfg.Blocks[work.Pop()].Predecessors)
                {
                    if (body.Add(pred))
                        work.Push(pred);
                }
            }
        }

        var follow = new Dictionary<int, int>();
        foreach (var (head, body) in bodies)
        {
            var exitTarget = -1;
            foreach (var node in body)
            {
                foreach (var succ in cfg.Blocks[node].Successors)
                {
                    if (body.Contains(succ) || succ == cfg.ExitIndex)
                        continue;
                    if (exitTarget == -1) exitTarget = succ;
                    else if (exitTarget != succ) return null;
                }
            }
            follow[head] = exitTarget == -1 ? cfg.ExitIndex : exitTarget;
        }

        return new LoopInfo(headers, follow);
    }
}
