using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCache;

public struct FVisibilitySample
{
    public TRange<float> Range;
    public bool bVisibilityState;
    
    public FVisibilitySample(FAssetArchive Ar)
    {
        Range = Ar.Read<TRange<float>>();
        bVisibilityState = Ar.ReadBoolean();
    }
}