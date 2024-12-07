using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.TL.Objects;

public class FSceneFaceDefSeamline : FStructFallback
{
    public FSceneFaceDefSeamline(FAssetArchive Ar) : base(Ar, "SceneFaceDefSeamline")
    {
        _ = Ar.Read<int>();
    }
}
