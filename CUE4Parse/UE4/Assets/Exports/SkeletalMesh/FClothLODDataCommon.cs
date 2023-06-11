using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FClothLODDataCommon : FStructFallback
{
    public readonly FMeshToMeshVertData[] TransitionDownSkinData = Array.Empty<FMeshToMeshVertData>();
    public readonly FMeshToMeshVertData[] TransitionUPSkinData = Array.Empty<FMeshToMeshVertData>();

    public FClothLODDataCommon() { }

    public FClothLODDataCommon(FAssetArchive Ar) : base(Ar, "ClothLODDataCommon")
    {
        TransitionUPSkinData = Ar.ReadArray(() => new FMeshToMeshVertData(Ar));
        TransitionDownSkinData = Ar.ReadArray(() => new FMeshToMeshVertData(Ar));
    }
}
