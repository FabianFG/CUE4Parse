using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public interface IAssetUserData
{
    public FPackageIndex[] AssetUserData { get; }
}
