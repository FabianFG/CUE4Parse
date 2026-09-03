using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public class FMaterialUniformExpression
{
    public IUStruct? Expression;

    // This is a pretty stupid thing but only can be done like this, maybe could be improved but shrug.
    public FMaterialUniformExpression(FAssetArchive Ar)
    {
        var expressionName = Ar.ReadFName().Text;

        Expression = expressionName switch
        {
            "FMaterialUniformExpressionVectorParameter"          => new FMaterialExpressionVectorParameter(Ar),
            "FMaterialUniformExpressionConstant"                 => new FMaterialUniformExpressionConstant(Ar),
            "FMaterialUniformExpressionScalarParameter"          => new FMaterialExpressionScalarParameter(Ar),
            "FMaterialUniformExpressionClamp"                    => new FMaterialUniformExpressionClamp(Ar),
            "FMaterialUniformExpressionFrac"                    => new FMaterialUniformExpressionFrac(Ar),
            "FMaterialUniformExpressionFoldedMath"               => new FMaterialUniformExpressionFoldedMath(Ar),
            "FMaterialUniformExpressionAppendVector"             => new FMaterialUniformExpressionAppendVector(Ar),
            "FMaterialUniformExpressionAbs"                      => new FMaterialUniformExpressionAbs(Ar),
            "FMaterialUniformExpressionCeil"                     => new FMaterialUniformExpressionCeil(Ar),
            "FMaterialUniformExpressionMax"                      => new FMaterialUniformExpressionMax(Ar),
            "FMaterialUniformExpressionMin"                      => new FMaterialUniformExpressionMin(Ar),
            "FMaterialUniformExpressionPeriodic"                 => new FMaterialUniformExpressionPeriodic(Ar),
            "FMaterialUniformExpressionSine"                     => new FMaterialUniformExpressionSine(Ar),
            "FMaterialUniformExpressionFlipBookTextureParameter" => new FMaterialUniformExpressionFlipBookTextureParameter(Ar),
            "FMaterialUniformExpressionTexture"                  => new FMaterialExpressionTextureBase(Ar),
            "FMaterialUniformExpressionTextureParameter"         => new FMaterialUniformExpressionTextureParameter(Ar),
            "FMaterialUniformExpressionRealTime"                 => null,
            "FMaterialUniformExpressionTime"                     => null,
            _ => UnknownExpression(expressionName)
        };
    }

    // If an unknown Expression is executed this will be logged multiple times, kinda annoying, but we shouldn't use throw.
    private static IUStruct? UnknownExpression(string name)
    {
        Log.Warning("Not defined " + name);
        return null;
    }
}
