using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.EdGraph;

// Custom class for easier serialization
public class UEdGraphPinReference(FAssetArchive Ar)
{
    /** The node that owns this pin. */
    public FPackageIndex OwningNode = new FPackageIndex(Ar);
    /** The pin's unique ID. */
    public FGuid PinId = Ar.Read<FGuid>();
}
