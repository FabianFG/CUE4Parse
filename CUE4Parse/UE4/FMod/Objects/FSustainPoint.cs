using System.Collections.Generic;
using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FSustainPoint
{
    public readonly uint Position;
    public readonly List<FEvaluator> Evaluators;

    public FSustainPoint(BinaryReader Ar)
    {
        Position = Ar.ReadUInt32();
        Evaluators = FEvaluator.ReadEvaluatorList(Ar);
    }
}
