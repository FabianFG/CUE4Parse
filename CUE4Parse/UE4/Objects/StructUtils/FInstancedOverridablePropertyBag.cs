using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
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
            // need to force TaggedSerialization
            if (Ar.HasUnversionedProperties) throw new ParserException(Ar, "FInstancedOverridablePropertyBag with unversioned properties is not supported in this version");
            Defaults = new FStructFallback(Ar, "InstancedOverridablePropertyBag");
            return;
        }

        OverridenPropertyIDs = Ar.ReadArray<FGuid>();
    }
}
