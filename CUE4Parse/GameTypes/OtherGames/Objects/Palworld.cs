using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Kismet;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public class EX_PalworldInstr1 : KismetExpression<string>
{
    public override EExprToken Token => EExprToken.EX_A2;

    public EX_PalworldInstr1(FKismetArchive Ar)
    {
        var value = Ar.Read<ushort>();
        if (Ar.Owner.Mappings != null && Ar.Owner.Mappings.Enums.TryGetValue("EPalWazaID", out var values) &&
            values.TryGetValue(value, out var member))
        {
            Value = string.Concat("EPalWazaID", "::", member);
        }
        else
        {
            Value = string.Concat("EPalWazaID", "::", value);
        }
    }
}
