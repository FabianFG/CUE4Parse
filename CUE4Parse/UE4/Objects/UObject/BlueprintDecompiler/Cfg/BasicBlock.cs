using System.Collections.Generic;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler.Cfg;

internal sealed class BasicBlock
{
    public readonly int Index;
    public readonly int Start;
    public readonly int End;
    public readonly List<int> Successors = [];
    public readonly List<int> Predecessors = [];

    public BasicBlock(int index, int start, int end)
    {
        Index = index;
        Start = start;
        End = end;
    }

    public bool IsExit => Start < 0;
}
