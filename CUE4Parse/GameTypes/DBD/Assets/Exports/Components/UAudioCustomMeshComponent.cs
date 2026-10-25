using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.DBD.Assets.Exports.Components;

public class UAudioCustomMeshComponent : UStaticMeshComponent
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is GAME_DeadByDaylight)
            bRenderInMainPass = false;
    }
}
