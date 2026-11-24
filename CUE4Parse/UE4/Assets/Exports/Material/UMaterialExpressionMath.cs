using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public class FMaterialUniformExpressionAbs : FMaterialUniformExpressionPeriodic
{
    public FMaterialUniformExpressionAbs(FAssetArchive Ar) : base(Ar) { }
}

public class FMaterialUniformExpressionCeil : FMaterialUniformExpressionPeriodic
{
    public FMaterialUniformExpressionCeil(FAssetArchive Ar) : base(Ar) { }
}
public class FMaterialUniformExpressionSquareRoot : FMaterialUniformExpressionPeriodic
{
    public FMaterialUniformExpressionSquareRoot(FAssetArchive Ar) : base(Ar) { }
}

public class FMaterialUniformExpressionPeriodic(FAssetArchive Ar) : IUStruct
{
    public FMaterialUniformExpression x { get; private set; } = new FMaterialUniformExpression(Ar);
}

public class FMaterialUniformExpressionSine(FAssetArchive Ar) : IUStruct
{
    public FMaterialUniformExpression x { get; private set; } = new FMaterialUniformExpression(Ar);
    public bool bIsCosine { get; private set; } = Ar.ReadBoolean();
}

public class FMaterialUniformExpressionClamp(FAssetArchive Ar) : IUStruct
{
    public FMaterialUniformExpression Input { get; private set; } = new FMaterialUniformExpression(Ar);
    public FMaterialUniformExpression Min { get; private set; } = new FMaterialUniformExpression(Ar);
    public FMaterialUniformExpression Max { get; private set; } = new FMaterialUniformExpression(Ar);
}

public class FMaterialUniformExpressionFrac(FAssetArchive Ar) : IUStruct
{
    public FMaterialUniformExpression X { get; private set; } = new FMaterialUniformExpression(Ar);
}

public class FMaterialUniformExpressionFoldedMath(FAssetArchive Ar) : IUStruct
{
    public FMaterialUniformExpression A { get; private set; } = new FMaterialUniformExpression(Ar);
    public FMaterialUniformExpression B { get; private set; } = new FMaterialUniformExpression(Ar);
    public byte Op { get; private set; } = Ar.Read<byte>();
}

public class FMaterialUniformExpressionMin : FMaterialUniformExpressionMax
{
    public FMaterialUniformExpressionMin(FAssetArchive Ar) : base(Ar) { }
}

public class FMaterialUniformExpressionMax(FAssetArchive Ar) : IUStruct
{
    public FMaterialUniformExpression A { get; private set; } = new FMaterialUniformExpression(Ar);
    public FMaterialUniformExpression B { get; private set; } = new FMaterialUniformExpression(Ar);
}

public class FMaterialUniformExpressionAppendVector(FAssetArchive Ar) : IUStruct
{
    public FMaterialUniformExpression A { get; private set; } = new FMaterialUniformExpression(Ar);
    public FMaterialUniformExpression B { get; private set; } = new FMaterialUniformExpression(Ar);
    public int NumComponentsA { get; private set; } = Ar.Read<int>();
}

public class FMaterialUniformExpressionConstant(FAssetArchive Ar) : IUStruct
{
    public FLinearColor Value { get; private set; } = Ar.Read<FLinearColor>();
    public byte ValueType { get; private set; } = Ar.Read<byte>();
}
