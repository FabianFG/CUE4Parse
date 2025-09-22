using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FClothLODDataCommon : FStructFallback
{
    public readonly FMeshToMeshVertData[] TransitionDownSkinData = [];
    public readonly FMeshToMeshVertData[] TransitionUPSkinData = [];

    public FClothLODDataCommon() { }

    public FClothLODDataCommon(FAssetArchive Ar) : base(Ar, "ClothLODDataCommon")
    {
        TransitionUPSkinData = Ar.ReadArray(() => new FMeshToMeshVertData(Ar));
        TransitionDownSkinData = Ar.ReadArray(() => new FMeshToMeshVertData(Ar));
        if (Ar.Game == EGame.GAME_Borderlands4) Ar.Position += 4;
    }
}
