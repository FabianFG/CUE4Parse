using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public sealed class FFastGeoArchive : FAssetArchive
{
    private FPackageIndex[] Assets;
    private FAssetArchive baseAr;

    public FFastGeoArchive(FAssetArchive Ar, FPackageIndex[] assets) : base(Ar, Ar.Owner, Ar.AbsoluteOffset)
    {
        Assets = assets;
        baseAr = Ar;
        Position = Ar.Position;
    }

    public FPackageIndex ReadFPackageIndex()
    {
        var index = Read<int>();
        if ( index == -1 ) return new FPackageIndex(baseAr, 0);
        if (index < 0 || index >= Assets.Length)
        {
            throw new ParserException($"Invalid FPackageIndex {index} (Assets count: {Assets.Length})");
        }
        return Assets[index];
    }
}
