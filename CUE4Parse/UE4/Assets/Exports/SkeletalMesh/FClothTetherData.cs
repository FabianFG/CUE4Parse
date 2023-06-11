using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FClothTetherData : FStructFallback
{
    public readonly (int, int, float)[][] Tethers = Array.Empty<(int, int, float)[]>();

    public FClothTetherData() { }

    public FClothTetherData(FAssetArchive Ar) : base(Ar, "ClothTetherData")
    {
        Tethers = Ar.ReadArray(() => Ar.ReadArray(() => (Ar.Read<int>(), Ar.Read<int>(), Ar.Read<float>())));
    }
}
