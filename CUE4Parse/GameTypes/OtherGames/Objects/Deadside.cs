using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public struct FSoundAttenuationPluginSettingsWithOverride : IUStruct
{
    public FPackageIndex[]? SpatializationPluginSettingsArray;
    public FPackageIndex[]? OcclusionPluginSettingsArray;
    public FPackageIndex[]? ReverbPluginSettingsArray;

    public FSoundAttenuationPluginSettingsWithOverride(FAssetArchive Ar)
    {
        if (Ar.ReadBoolean())
        {
            SpatializationPluginSettingsArray = Ar.ReadArray(() => new FPackageIndex(Ar));
            OcclusionPluginSettingsArray = Ar.ReadArray(() => new FPackageIndex(Ar));
            ReverbPluginSettingsArray = Ar.ReadArray(() => new FPackageIndex(Ar));
        }

    }
}
