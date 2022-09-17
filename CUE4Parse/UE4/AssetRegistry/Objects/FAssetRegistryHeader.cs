using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.AssetRegistry.Objects;

public class FAssetRegistryHeader
{
    public FAssetRegistryVersionType Version;
    public bool bFilterEditorOnlyData;

    public FAssetRegistryHeader(FArchive Ar)
    {
        FAssetRegistryVersion.TrySerializeVersion(Ar, out Version);
        bFilterEditorOnlyData = Version >= FAssetRegistryVersionType.AddedHeader && Ar.ReadBoolean();
    }

    public FAssetRegistryHeader(FAssetRegistryVersionType version, bool filterEditorOnlyData)
    {
        Version = version;
        bFilterEditorOnlyData = filterEditorOnlyData;
    }
}
