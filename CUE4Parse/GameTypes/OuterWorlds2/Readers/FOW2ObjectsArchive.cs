using CUE4Parse.GameTypes.OuterWorlds2.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.OuterWorlds2.Readers;

public class FOW2ObjectsArchive : FObjectAndNameAsStringProxyArchive
{
    public readonly FPropertryDataObjectContainer Objects;
    public readonly IPackage Asset;
    public readonly bool bHasVersion;

    public FOW2ObjectsArchive(FAssetArchive Ar, FPropertryDataObjectContainer container) : base(Ar)
    {
        Objects = container;
        Asset = Ar.Owner!;
    }

    public FOW2ObjectsArchive(FArchive Ar, FPropertryDataObjectContainer container) : base(Ar)
    {
        Objects = container;
        Asset = null!;
    }

    public FOW2ObjectsArchive(FArchive Ar, IPackage owner, FPropertryDataObjectContainer container, bool hasVersion, int absoluteOffset = 0) : base(Ar, null, absoluteOffset)
    {
        Asset = owner;
        Objects = container;
        bHasVersion = hasVersion;
        Ver = hasVersion ? new FPackageFileVersion(Read<int>(), Read<int>()) : new FPackageFileVersion(522, 1010);
    }
}
