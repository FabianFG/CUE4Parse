using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Kismet;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.DFHO.Kismet;

public class EX_DFInstr : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_6E;
    public KismetExpression Left;
    public KismetExpression Right;

    public EX_DFInstr(FKismetArchive Ar)
    {
        Left = Ar.ReadExpression();
        Right = Ar.ReadExpression();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName(nameof(Left));
        serializer.Serialize(writer, Left);
        writer.WritePropertyName(nameof(Right));
        serializer.Serialize(writer, Right);
    }
}
