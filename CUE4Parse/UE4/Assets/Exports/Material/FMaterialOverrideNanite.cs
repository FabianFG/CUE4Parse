using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Material;

[StructFallback]
public readonly struct FMaterialOverrideNanite : IUStruct
{
    public readonly FSoftObjectPath OverrideMaterialRef;
    public readonly bool bEnableOverride;
    public readonly FPackageIndex? OverrideMaterial;

    public FMaterialOverrideNanite(FStructFallback fallback)
    {
        OverrideMaterialRef = fallback.GetOrDefault(nameof(OverrideMaterialRef), default(FSoftObjectPath));
        bEnableOverride = fallback.GetOrDefault(nameof(bEnableOverride), true);
        OverrideMaterial = fallback.GetOrDefault(nameof(OverrideMaterial), new FPackageIndex());
    }

    public FMaterialOverrideNanite(FAssetArchive Ar)
    {
        if (FFortniteReleaseBranchCustomObjectVersion.Get(Ar) < FFortniteReleaseBranchCustomObjectVersion.Type.NaniteMaterialOverrideUsesEditorOnly)
        {
            OverrideMaterialRef = new FSoftObjectPath(Ar);
            bEnableOverride = Ar.ReadBoolean();
            OverrideMaterial = new FPackageIndex(Ar);
            return;
        }

        var bSerializeAsCookedData = Ar.ReadBoolean();

        if (bSerializeAsCookedData)
        {
            OverrideMaterial = new FPackageIndex(Ar);
        }

        // Serialized properties
        var fallback = new FMaterialOverrideNanite(new FStructFallback(Ar, "MaterialOverrideNanite"));
        OverrideMaterialRef = fallback.OverrideMaterialRef;
        bEnableOverride = fallback.bEnableOverride;
    }
}
