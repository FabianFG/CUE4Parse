using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine;

[StructFallback][JsonConverter(typeof(FMaterialInputConverter<>))]
public class FMaterialInput<T> : FExpressionInput where T : struct
{
    public bool UseConstant { get; protected set; }
    public T Constant { get; protected set; }

    public FMaterialInput()
    {
        UseConstant = false;
        Constant = new T();
    }

    public FMaterialInput(FAssetArchive Ar) : base(Ar)
    {
        if (FCoreObjectVersion.Get(Ar) < FCoreObjectVersion.Type.MaterialInputNativeSerialize)
        {
            return;
        }

        UseConstant = Ar.ReadBoolean();
        Constant = Ar.Read<T>();
    }
}

public class FMaterialInputConverter<T> : JsonConverter<FMaterialInput<T>> where T : struct
{
    public override void WriteJson(JsonWriter writer, FMaterialInput<T> value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        if (value.FallbackStruct is null)
        {
            writer.WritePropertyName(nameof(value.Expression));
            serializer.Serialize(writer, value.Expression);
            writer.WritePropertyName(nameof(value.OutputIndex));
            serializer.Serialize(writer, value.OutputIndex);
            writer.WritePropertyName(nameof(value.InputName));
            serializer.Serialize(writer, value.InputName);
            writer.WritePropertyName(nameof(value.Mask));
            serializer.Serialize(writer, value.Mask);
            writer.WritePropertyName(nameof(value.MaskR));
            serializer.Serialize(writer, value.MaskR);
            writer.WritePropertyName(nameof(value.MaskG));
            serializer.Serialize(writer, value.MaskG);
            writer.WritePropertyName(nameof(value.MaskB));
            serializer.Serialize(writer, value.MaskB);
            writer.WritePropertyName(nameof(value.MaskA));
            serializer.Serialize(writer, value.MaskA);
            writer.WritePropertyName(nameof(value.ExpressionName));
            serializer.Serialize(writer, value.ExpressionName);
            writer.WritePropertyName(nameof(value.UseConstant));
            serializer.Serialize(writer, value.UseConstant);
            writer.WritePropertyName(nameof(value.Constant));
            serializer.Serialize(writer, value.Constant);
        }
        else
        {
            writer.WritePropertyName(nameof(value.FallbackStruct));
            serializer.Serialize(writer, value.FallbackStruct);
        }
        writer.WriteEndObject();
    }

    public override FMaterialInput<T>? ReadJson(JsonReader reader, Type objectType, FMaterialInput<T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

[StructFallback]
public class FMaterialInputVector : FMaterialInput<FVector>
{
    public FMaterialInputVector()
    {
        UseConstant = false;
        Constant = FVector.ZeroVector;
    }
}

[StructFallback]
public class FMaterialInputVector2D : FMaterialInput<FVector2D>
{
    public FMaterialInputVector2D()
    {
        UseConstant = false;
        Constant = FVector2D.ZeroVector;
    }
}

[StructFallback][JsonConverter(typeof(FExpressionInputConverter))]
public class FExpressionInput : IUStruct
{
    public FPackageIndex? Expression;
    public int OutputIndex;
    public FName InputName;
    public int Mask;
    public int MaskR;
    public int MaskG;
    public int MaskB;
    public int MaskA;
    public FName ExpressionName;
    public FStructFallback? FallbackStruct;

    public FExpressionInput() { }

    public FExpressionInput(FAssetArchive Ar)
    {
        if (FCoreObjectVersion.Get(Ar) < FCoreObjectVersion.Type.MaterialInputNativeSerialize)
        {
            FallbackStruct  = new FStructFallback(Ar);
            return;
        }

        if (Ar is { Game: < EGame.GAME_UE5_1, IsFilterEditorOnly: false } || Ar.Game >= EGame.GAME_UE5_1)
            Expression = new FPackageIndex(Ar);
        OutputIndex = Ar.Read<int>();
        InputName = FFrameworkObjectVersion.Get(Ar) >= FFrameworkObjectVersion.Type.PinsStoreFName ? Ar.ReadFName() : new FName(Ar.ReadFString());
        Mask = Ar.Read<int>();
        MaskR = Ar.Read<int>();
        MaskG = Ar.Read<int>();
        MaskB = Ar.Read<int>();
        MaskA = Ar.Read<int>();
        ExpressionName = Ar is { Game: <= EGame.GAME_UE5_1, IsFilterEditorOnly: true } ? Ar.ReadFName() : (Expression ?? new FPackageIndex()).Name.SubstringAfterLast('/');
    }
}

public class FExpressionInputConverter : JsonConverter<FExpressionInput>
{
    public override void WriteJson(JsonWriter writer, FExpressionInput value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        if (value.FallbackStruct is null)
        {
            writer.WritePropertyName(nameof(value.Expression));
            serializer.Serialize(writer, value.Expression);
            writer.WritePropertyName(nameof(value.OutputIndex));
            serializer.Serialize(writer, value.OutputIndex);
            writer.WritePropertyName(nameof(value.InputName));
            serializer.Serialize(writer, value.InputName);
            writer.WritePropertyName(nameof(value.Mask));
            serializer.Serialize(writer, value.Mask);
            writer.WritePropertyName(nameof(value.MaskR));
            serializer.Serialize(writer, value.MaskR);
            writer.WritePropertyName(nameof(value.MaskG));
            serializer.Serialize(writer, value.MaskG);
            writer.WritePropertyName(nameof(value.MaskB));
            serializer.Serialize(writer, value.MaskB);
            writer.WritePropertyName(nameof(value.MaskA));
            serializer.Serialize(writer, value.MaskA);
            writer.WritePropertyName(nameof(value.ExpressionName));
            serializer.Serialize(writer, value.ExpressionName);
        }
        else
        {
            writer.WritePropertyName(nameof(value.FallbackStruct));
            serializer.Serialize(writer, value.FallbackStruct);
        }

        writer.WriteEndObject();
    }

    public override FExpressionInput? ReadJson(JsonReader reader, Type objectType, FExpressionInput? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class UMaterialExpression : Assets.Exports.UObject
{
    public FPackageIndex? Material { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Material = GetOrDefault<FPackageIndex>(nameof(Material));
    }
}
