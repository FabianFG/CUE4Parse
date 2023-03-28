using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Material.Editor;

public class UMaterialInstanceEditorOnlyData : UMaterialInterfaceEditorOnlyData
{
    public FStaticParameterSet? StaticParameters;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        StaticParameters = Get<FStaticParameterSet>(nameof(StaticParameters));
    }
}