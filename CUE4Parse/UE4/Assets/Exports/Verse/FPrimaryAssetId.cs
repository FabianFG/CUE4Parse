using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Verse;

// This identifies an object as a "primary" asset that can be searched for by the AssetManager and used in various tools
public struct FPrimaryAssetId
{
    /** An FName describing the logical type of this object, usually the name of a base UClass. For example, any Blueprint derived from APawn will have a Primary Asset Type of "Pawn".
    "PrimaryAssetType:PrimaryAssetName" should form a unique name across your project. */
    public FName PrimaryAssetType; // FPrimaryAssetType type, which is FName
    /** An FName describing this asset. This is usually the short name of the object, but could be a full asset path for things like maps, or objects with GetPrimaryId() overridden.
    "PrimaryAssetType:PrimaryAssetName" should form a unique name across your project. */
    public FName PrimaryAssetName;

    public FPrimaryAssetId(FAssetArchive Ar)
    {
        PrimaryAssetType = Ar.ReadFName();
        PrimaryAssetName = Ar.ReadFName();
    }
}
