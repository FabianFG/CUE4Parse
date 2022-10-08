using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Engine
{
    public class UFortHomebaseNodeGameplayEffectDataTable : UDataTable
    {
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            RowStructName = "HomebaseNodeGameplayEffectDataTableRow";
            base.Deserialize(Ar, validPos);
        }
    }
}
