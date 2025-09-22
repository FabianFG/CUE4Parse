using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.WuWa.Kismet;

public class EX_WuWaInstr1 : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_6E;
    public FVector Pos1;
    public FVector Pos2;

    public EX_WuWaInstr1(FKismetArchive Ar)
    {
        Pos1 = Ar.Read<FVector>();
        Pos2 = Ar.Read<FVector>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Pos1");
        serializer.Serialize(writer, Pos1);
        writer.WritePropertyName("Pos2");
        serializer.Serialize(writer, Pos2);
    }
}

public class EX_WuWaInstr2 : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_6F;

    public FQuat Rotation;
    public FVector Pos1;
    public FVector Pos2;
    public FVector Scale;

    public EX_WuWaInstr2(FKismetArchive Ar)
    {
        Rotation = Ar.Read<FQuat>();
        Pos1 = Ar.Read<FVector>();
        Pos2 = Ar.Read<FVector>();
        Scale = Ar.Read<FVector>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName("Rotation");
        serializer.Serialize(writer, Rotation);
        writer.WritePropertyName("Pos1");
        serializer.Serialize(writer, Pos1);
        writer.WritePropertyName("Pos2");
        serializer.Serialize(writer, Pos2);
        writer.WritePropertyName("Scale");
        serializer.Serialize(writer, Scale);
    }
}
