using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.AssetRegistry.Objects;

public class FPartialAssetData
{
    public readonly FName PackageName;
    public readonly FName AssetName;
    public readonly FName AssetClass;

    public FPartialAssetData(FAssetRegistryArchive Ar)
    {
        if (Ar.Header.Version < FAssetRegistryVersionType.RemoveAssetPathFNames)
            Ar.SkipFName();

        Ar.SkipFName();
        AssetClass = Ar.Header.Version >= FAssetRegistryVersionType.ClassPaths ? new FTopLevelAssetPath(Ar).AssetName : Ar.ReadFName();

        if (Ar.Header.Version < FAssetRegistryVersionType.RemovedMD5Hash)
            Ar.SkipFName();

        PackageName = Ar.ReadFName();
        AssetName = Ar.ReadFName();
        if (Ar.Header.Version >= FAssetRegistryVersionType.RemoveAssetPathFNames && !Ar.IsFilterEditorOnly)
            Ar.SkipFName();

        Ar.SkipTagsAndBundles();

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.CHANGED_CHUNKID_TO_BE_AN_ARRAY_OF_CHUNKIDS)
            Ar.SkipFixedArray(sizeof(int));
        else if (Ar.Ver >= EUnrealEngineObjectUE4Version.ADDED_CHUNKID_TO_ASSETDATA_AND_UPACKAGE)
            Ar.Position += sizeof(int);

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.COOKED_ASSETS_IN_EDITOR_SUPPORT)
            Ar.Position += sizeof(uint);
    }
}
