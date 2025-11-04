using CUE4Parse.GameTypes.OuterWorlds2.Readers;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.OuterWorlds2.Objects;

public class FPropertyDataWithObjects : FStructFallback
{
    public FPropertyDataWithObjects(FAssetArchive Ar, byte type)
    {
        var bHasVersion = Ar is FOW2ObjectsArchive OW2Ar && !OW2Ar.bHasVersion ? false : Ar.ReadBoolean();
        var data = Ar.ReadArray<byte>();
        var objects = new FPropertryDataObjectContainer(Ar);
        using var byteAr = new FByteArchive("FPropertyDataWithObjects", data, Ar.Versions);
        using var objectAr = new FOW2ObjectsArchive(byteAr, Ar.Owner, objects, bHasVersion);
        UObject.DeserializePropertiesTagged(Properties, objectAr, false);
    }
}
