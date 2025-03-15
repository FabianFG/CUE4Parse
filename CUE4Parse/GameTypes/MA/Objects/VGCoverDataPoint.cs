using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.MA.Objects;

// Metro Awakening
public class VGCoverDataPoint : FStructFallback
{
    public VGCoverDataPoint(FAssetArchive Ar) : base(Ar, "VGCoverDataPoint")
    {
        Ar.Position += 32;
        Ar.SkipFixedArray(24);
        Ar.SkipFixedArray(8);
    }
}
