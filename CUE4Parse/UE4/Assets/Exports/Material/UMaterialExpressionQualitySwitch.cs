using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public class UMaterialExpressionQualitySwitch : UMaterialExpression
{
    public FExpressionInput[] Inputs = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (!TryGetAllValues(out Inputs, nameof(Inputs)))
        {
            Inputs = new FExpressionInput[(int) EMaterialQualityLevel.Num];
        }
    }
}
