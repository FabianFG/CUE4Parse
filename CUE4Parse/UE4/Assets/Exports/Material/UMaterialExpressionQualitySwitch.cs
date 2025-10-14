using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public class UMaterialExpressionQualitySwitch : UMaterialExpression
{
    public List<FExpressionInput> Inputs = new((int) EMaterialQualityLevel.Num);

    public UMaterialExpressionQualitySwitch()
    {
        for (int i = 0; i < (int) EMaterialQualityLevel.Num; i++)
        {
            Inputs.Add(null!);
        }
    }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        foreach (var property in Properties)
        {
            if (property.Name.Text != "Inputs") continue;
            if (property.Tag?.GenericValue is not FScriptStruct { StructType: FExpressionInput input }) continue;

            var index = property.ArrayIndex;
            if (index < 0) continue;

            if (index >= Inputs.Count)
            {
                Inputs.AddRange(new FExpressionInput?[index - Inputs.Count + 1]!);
            }

            Inputs[index] = input;
        }
    }
}
