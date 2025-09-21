using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Kismet;
using FixedMathSharp;

namespace CUE4Parse.GameTypes._2XKO.Kismet;

public class EX_FixedPointConst : KismetExpression<Fixed64>
{
    public override EExprToken Token => EExprToken.EX_FD;

    public EX_FixedPointConst(FKismetArchive Ar)
    {
        Value = Ar.Read<Fixed64>();
    }
}
