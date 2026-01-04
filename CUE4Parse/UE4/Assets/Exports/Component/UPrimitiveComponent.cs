using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UPrimitiveComponent : USceneComponent
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if(Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 16;
    }

    public virtual UBodySetup? GetBodySetup() => null;
}
