using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Chaos.GeometryCollection;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.HairStrands;

[JsonConverter(typeof(FGroomDataflowSettingsConverter))]
public class FGroomDataflowSettings : FStructFallback
{
    /** Rest collection used to store all the dataflow attributes */
    public FManagedArrayCollection RestCollection;

    public FGroomDataflowSettings(FAssetArchive Ar) : base(Ar, "GroomDataflowSettings")
    {
        RestCollection = new FManagedArrayCollection(new(Ar));
    }
}
