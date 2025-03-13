using CUE4Parse.GameTypes.FF7.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.FF7.Assets.Exports;

public class UMemoryMappedAsset : UObject
{
    protected FMemoryMappedImageResult FrozenArchive;
    public FMemoryMappedImageArchive InnerArchive;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        FrozenArchive = new FMemoryMappedImageResult();
        FrozenArchive.LoadFromArchive(Ar);
        InnerArchive = new FMemoryMappedImageArchive(new FByteArchive("MemoryMappedAsset", FrozenArchive.FrozenObject, Ar.Versions))
        {
            Names = FrozenArchive.GetNames()
        };
    }
}
