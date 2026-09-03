using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public class FMaterialUniformExpressionAbs : FMaterialUniformExpressionPeriodic
{
    public FMaterialUniformExpressionAbs(FAssetArchive Ar)
        : base(Ar)
    {
    }
}

public class FMaterialUniformExpressionCeil : FMaterialUniformExpressionPeriodic
{
    public FMaterialUniformExpressionCeil(FAssetArchive Ar)
        : base(Ar)
    {
    }
}
public class FMaterialUniformExpressionSquareRoot : FMaterialUniformExpressionPeriodic
{
    public FMaterialUniformExpressionSquareRoot(FAssetArchive Ar)
        : base(Ar)
    {
    }
}

public class FMaterialUniformExpressionPeriodic : IUStruct
{
    public FMaterialUniformExpression x { get; private set; }

    public FMaterialUniformExpressionPeriodic(FAssetArchive Ar)
    {
        x = new FMaterialUniformExpression(Ar);
    }
}

public class FMaterialUniformExpressionSine : IUStruct
{
    public FMaterialUniformExpression x { get; private set; }
    public bool bIsCosine { get; private set; }

    public FMaterialUniformExpressionSine(FAssetArchive Ar)
    {
        x = new FMaterialUniformExpression(Ar);
        bIsCosine = Ar.ReadBoolean();
    }
}

public class FMaterialUniformExpressionClamp : IUStruct
{
    public FMaterialUniformExpression Input { get; private set; }
    public FMaterialUniformExpression Min { get; private set; }
    public FMaterialUniformExpression Max { get; private set; }

    public FMaterialUniformExpressionClamp(FAssetArchive Ar)
    {
        Input = new FMaterialUniformExpression(Ar);
        Min = new FMaterialUniformExpression(Ar);
        Max = new FMaterialUniformExpression(Ar);
    }
}

public class FMaterialUniformExpressionFrac : IUStruct
{
    public FMaterialUniformExpression X { get; private set; }

    public FMaterialUniformExpressionFrac(FAssetArchive Ar)
    {
        X = new FMaterialUniformExpression(Ar);
    }
}

public class FMaterialUniformExpressionFoldedMath : IUStruct
{
    public FMaterialUniformExpression A { get; private set; }
    public FMaterialUniformExpression B { get; private set; }
    public byte Op { get; private set; }

    public FMaterialUniformExpressionFoldedMath(FAssetArchive Ar)
    {
        A = new FMaterialUniformExpression(Ar);
        B = new FMaterialUniformExpression(Ar);
        Op = Ar.Read<byte>();
    }
}

public class FMaterialUniformExpressionMin : FMaterialUniformExpressionMax
{
    public FMaterialUniformExpressionMin(FAssetArchive Ar)
        : base(Ar)
    {
    }
}

public class FMaterialUniformExpressionMax : IUStruct
{
    public FMaterialUniformExpression A { get; private set; }
    public FMaterialUniformExpression B { get; private set; }

    public FMaterialUniformExpressionMax(FAssetArchive Ar)
    {
        A = new FMaterialUniformExpression(Ar);
        B = new FMaterialUniformExpression(Ar);
    }
}

public class FMaterialUniformExpressionAppendVector : IUStruct
{
    public FMaterialUniformExpression A { get; private set; }
    public FMaterialUniformExpression B { get; private set; }
    public int NumComponentsA { get; private set; }

    public FMaterialUniformExpressionAppendVector(FAssetArchive Ar)
    {
        A = new FMaterialUniformExpression(Ar);
        B = new FMaterialUniformExpression(Ar);
        NumComponentsA = Ar.Read<int>();
    }
}

public class FMaterialUniformExpressionConstant : IUStruct
{
    public FLinearColor Value { get; private set; }
    public byte ValueType { get; private set; }

    public FMaterialUniformExpressionConstant(FAssetArchive Ar)
    {
        Value = Ar.Read<FLinearColor>();
        ValueType = Ar.Read<byte>();
    }
}
