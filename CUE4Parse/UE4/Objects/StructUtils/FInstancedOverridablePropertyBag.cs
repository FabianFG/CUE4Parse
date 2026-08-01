using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.StructUtils;

public class FInstancedOverridablePropertyBag : FInstancedPropertyBag
{
    public FStructFallback? Defaults;
    public FGuid[]? OverridenPropertyIDs;

    public FInstancedOverridablePropertyBag(FAssetArchive Ar) : base(Ar)
    {
        if (FOverridablePropertyBagCustomVersion.Get(Ar) < FOverridablePropertyBagCustomVersion.Type.FixSerializer)
        {
            if (Ar is { HasUnversionedProperties: true, Owner: not null })
            {
                try
                {
                    Ar.Owner.Summary.PackageFlags &= ~EPackageFlags.PKG_UnversionedProperties;
                    Defaults = new FStructFallback(Ar, "InstancedOverridablePropertyBag");
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Failed to serialize FInstancedOverridablePropertyBag before FixSerializer version");
                    throw;
                }
                finally
                {
                    Ar.Owner.Summary.PackageFlags |= EPackageFlags.PKG_UnversionedProperties;
                }
            }
            else
            {
                Defaults = new FStructFallback(Ar, "InstancedOverridablePropertyBag");
            }
            return;
        }

        OverridenPropertyIDs = Ar.ReadArray<FGuid>();
    }
}
