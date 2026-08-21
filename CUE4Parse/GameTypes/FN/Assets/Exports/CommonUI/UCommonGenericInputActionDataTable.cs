using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.CommonUI
{
    [ObjectType("/Script/CommonUI.CommonGenericInputActionDataTable")]
    public class UCommonGenericInputActionDataTable : UDataTable
    {
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            RowStructName = "CommonInputActionDataBase";
            RowStructIdentifier = "/Script/CommonUI.CommonInputActionDataBase";
            base.Deserialize(Ar, validPos);
        }
    }
}
