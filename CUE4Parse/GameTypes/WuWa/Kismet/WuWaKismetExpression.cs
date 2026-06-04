using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.WuWa.Kismet;

public class EX_WuWaInstr1(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_6E;

    public FVector Pos1 = Ar.Read<FVector>();
    public FVector Pos2 = Ar.Read<FVector>();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName(nameof(Pos1));
        serializer.Serialize(writer, Pos1);
        writer.WritePropertyName(nameof(Pos2));
        serializer.Serialize(writer, Pos2);
    }

    public override string ToString() => $"(Pos1: {Pos1}, Pos2: {Pos2})";
}

public class EX_WuWaInstr2(FKismetArchive Ar) : KismetExpression
{
    public override EExprToken Token => EExprToken.EX_6F;

    public FQuat Rotation = Ar.Read<FQuat>();
    public FVector Pos1 = Ar.Read<FVector>();
    public FVector Pos2 = Ar.Read<FVector>();
    public FVector Scale = Ar.Read<FVector>();

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer, bool bAddIndex = false)
    {
        base.WriteJson(writer, serializer, bAddIndex);
        writer.WritePropertyName(nameof(Rotation));
        serializer.Serialize(writer, Rotation);
        writer.WritePropertyName(nameof(Pos1));
        serializer.Serialize(writer, Pos1);
        writer.WritePropertyName(nameof(Pos2));
        serializer.Serialize(writer, Pos2);
        writer.WritePropertyName(nameof(Scale));
        serializer.Serialize(writer, Scale);
    }

    public override string ToString() => $"(Rotation: {Rotation}, Pos1: {Pos1}, Pos2: {Pos2}, Scale: {Scale})";
}
