using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.CommonUI
{
    public class UCommonGenericInputActionDataTable : UDataTable
    {
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            RowStructName = "CommonInputActionDataBase";
            base.Deserialize(Ar, validPos);
        }
    }
}
