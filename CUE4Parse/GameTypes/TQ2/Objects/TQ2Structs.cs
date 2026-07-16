using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using Serilog;

namespace CUE4Parse.GameTypes.TQ2.Objects;

public static class TQ2Structs
{
    public static IUStruct ParseTQ2Struct(FAssetArchive Ar, string? structName, UStruct? struc, ReadType? type)
    {
        if (structName is null) return type == ReadType.ZERO ? new FStructFallback() : struc != null ? new FStructFallback(Ar, struc) : new FStructFallback(Ar, structName);
        return structName switch
        {
            "GrimItemDescriptionPtr" or "GrimAbilityProjectileParametersPtr" or "SoftJsonDataAssetPtr" or
                "GrimAbilitySummonDataPtr" or "JsonDataAssetPath" or "JsonDataAssetPtr" => new FSoftObjectPath(Ar),
            "GrimDialogueVariableValue" => new FGrimDialogueVariableValue(Ar),
            "GrimArchetypeDeformationExpressionVector" => new FGrimUnrealExpression(Ar, structName, false, ETQ2ConstantType.Vector),
            "GrimArchetypeCategoryExpression" => new FGrimUnrealExpression(Ar, structName, false, ETQ2ConstantType.Name),
            "GrimItemValue" or "GrimDynamicsFormula" or "GrimAbilityDynamicsFormula" => new FGrimUnrealExpression(Ar, structName, true, ETQ2ConstantType.Float),
            "GrimArchetypeMaterialExpressionFloat" or "GrimArchetypeDeformationExpressionFloat" => new FGrimUnrealExpression(Ar, structName, false, ETQ2ConstantType.Float),
            _ when structName.StartsWith("GrimDialogue") && structName.EndsWith("Ref") => new FArticyId(Ar),
            _ when structName.StartsWith("TQ2") && structName.EndsWith("Ptr") => new FSoftObjectPath(Ar),
            _ when structName.StartsWith("Grim") && structName.EndsWith("SoftPtr") => new FSoftObjectPath(Ar),
            _ when structName.StartsWith("Grim") && structName.EndsWith("Ptr") => new FGrimInstancedObjectPtr(Ar),
            _ => type == ReadType.ZERO ? new FStructFallback() : struc != null ? new FStructFallback(Ar, struc) : new FStructFallback(Ar, structName)
        };
    }
}

public class FGrimUnrealExpression : IUStruct
{
    public FStructFallback Struct;
    public FPackageIndex Owner;
    public FPackageIndex StructType;

    public FName PropertyName;
    public string SourceText;
    public object? Value;
    public FPackageIndex? Instance;

    public FGrimUnrealExpression(FAssetArchive Ar, string type, bool readTemplate, ETQ2ConstantType readtype)
    {
        Struct = new FStructFallback(Ar, type);

        var idk1 = Ar.Read<int>(); // actually 0 or 1 in most case and 2 in one asset
        var idk2 = Ar.ReadBoolean();
        Owner = new FPackageIndex(Ar);
        StructType = new FPackageIndex(Ar);
        PropertyName = Ar.ReadFName();
        SourceText = Ar.ReadFString();

        if (StructType.IsNull)
        {
            return;
        }
        if (PropertyName.IsNone || idk1 == 2)
        {
            Instance = new FPackageIndex(Ar);
            return;
        }

        Value = FTQ2Expression.ReadFTQ2ExpressionNode(Ar, readtype);       
        Instance = readTemplate ? new FPackageIndex(Ar) : new FPackageIndex();
    }
}

public abstract class FTQ2Expression
{
    public static FTQ2Expression? ReadFTQ2ExpressionNode(FAssetArchive Ar, ETQ2ConstantType readtype)
    {
        var type = Ar.Read<ushort>();
        return type switch
        {
            0 => new FTQ2ConstantExpression(Ar, readtype),
            1 => new FTQ2PropertyPathExpression(Ar),
            2 => new FTQ2OperationExpression(Ar),
            3 => new FTQ2FunctionExpression(Ar),
            4 => new FTQ2FormulaExpression(Ar, readtype),
            256 => new FTQ2ConstantExpression(Ar, readtype), // zero value for  left hand side - operator
            _ => throw new ParserException($"Unknwown FTQ2Expression type {type}"),
        };
    }
}

public class FTQ2PropertyPathExpression : FTQ2Expression
{
    public FName Base;
    public FName Path;
    public byte type;

    public FTQ2PropertyPathExpression(FAssetArchive Ar)
    {
        var test = Ar.Read<int>(); 
        if (test < 0 || test > Ar.Owner!.NameMap.Length)
        {
            Ar.Position -= 2;
            Path = Ar.ReadFName().Text;
            return;
        }

        // idk how to identify them properly
        Ar.Position += 8;
        var test2 = Ar.Read<byte>();
        Ar.Position -= 13;
        if (test == 3 && test2 != 0)
        {
            Base = "None";
            Ar.Position += 8;
            Path = Ar.ReadFString();
            type = Ar.Read<byte>();
        }
        else
        {
            Base = Ar.ReadFName();
            type = Ar.Read<byte>();
            Path = Ar.ReadFName();
        }
    }
}

public enum ETQ2ConstantType
{
    Float,
    Name,
    Vector
}

public class FTQ2ConstantExpression : FTQ2Expression
{
    public object Value;

    public FTQ2ConstantExpression(FAssetArchive Ar, ETQ2ConstantType readtype)
    {
        // edge case in at_centaur_female with type 256 with fvector instead of float
        Value = readtype switch
        {
            ETQ2ConstantType.Vector => new FVector(Ar),
            ETQ2ConstantType.Name => Ar.ReadFName(),
            _ => Ar.Read<float>(),
        };
    }
}

public class FTQ2OperationExpression : FTQ2Expression
{
    public int LeftHandOperandCount;
    public int AdditionalOperandCount;
    public int RightHandOperandCount;
    public string Token;

    public FTQ2OperationExpression(FAssetArchive Ar)
    {
        // goes from right to left, so AdditionalOperands -> RightHandOperandCount -> LeftHandOperandCount
        LeftHandOperandCount = Ar.Read<int>();
        AdditionalOperandCount = Ar.Read<int>();
        RightHandOperandCount = Ar.Read<int>();
        Token = Ar.ReadFString();
    }
}

public class FTQ2FunctionExpression : FTQ2OperationExpression
{
    public (string, string)[] DefaultValues;
    public string Path;

    public FTQ2FunctionExpression(FAssetArchive Ar) : base(Ar)
    {
        DefaultValues = Ar.ReadArray(() => (Ar.ReadFString(), Ar.ReadFString()));
        var part1 = Ar.ReadFName();
        var unknown = Ar.Read<byte>(); // always 2
        var part2 = Ar.ReadFName();
        Path = $"{part1.Text}.{part2.Text}";
    }
}

public class FTQ2FormulaExpression : FTQ2Expression
{
    public int UnknownCount;
    public int ExpressionsCount;
    public FTQ2Expression?[] Expressions;

    public FTQ2FormulaExpression(FAssetArchive Ar, ETQ2ConstantType readtype)
    {
        UnknownCount = Ar.Read<int>(); // or index?
        ExpressionsCount = Ar.Read<int>();
        Expressions = Ar.ReadArray(ExpressionsCount, () => ReadFTQ2ExpressionNode(Ar, readtype));
    }
}

public enum EGrimDialogueVariableType : byte
{
    Bool,
    Int,
    Float,
    String,
    NumOf,
}

public class FGrimDialogueVariableValue : IUStruct
{
    public EGrimDialogueVariableType Type;
    public dynamic Value;

    public FGrimDialogueVariableValue(FAssetArchive Ar)
    {
        Type = Ar.Read<EGrimDialogueVariableType>();
        Value = Type switch
        {
            EGrimDialogueVariableType.Bool => Ar.ReadBoolean(),
            EGrimDialogueVariableType.Int => (Ar.Read<int>(), Ar.Read<int>()),
            _ => throw new ParserException($"Unsupported GrimDialogueVariableType: {Type}"),
        };
    }
}

public struct FArticyId(FAssetArchive Ar) : IUStruct
{
    public ulong ID = Ar.Read<ulong>();
}

public class FGrimInstancedObjectPtr : IUStruct
{
    public int Type;
    public int Index;
    public FPackageIndex StructType;
    public FStructFallback? NonConstStruct;

    public FGrimInstancedObjectPtr(FAssetArchive Ar)
    {
        var bValid = Ar.ReadBoolean();
        StructType = new FPackageIndex(Ar);
        if (StructType.IsNull || !bValid) return;

        Type = Ar.Read<int>();
        Index = Ar.Read<int>();

        if (StructType.TryLoad<UStruct>(out var struc))
        {
            NonConstStruct = new FStructFallback(Ar, struc);
        }
        else if (StructType.ResolvedObject is { } obj)
        {
            NonConstStruct = new FStructFallback(Ar, obj.Name.ToString());
        }
        else
        {
            Log.Warning("Failed to read FGrimInstancedObjectPtr of type {0}, skipping it", StructType.ResolvedObject?.GetFullName());
        }

        Ar.Position += 4; // zero
    }
}
