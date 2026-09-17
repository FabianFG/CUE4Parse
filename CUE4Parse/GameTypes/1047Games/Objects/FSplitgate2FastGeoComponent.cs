using CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes._1047Games.Objects;

public class FSplitgate2FastGeoComponent : FFastGeoComponent
{
    public FVector Vector;
    public FPackageIndex Material;
    public FTransform Transform;
    public FBoxSphereBounds Bounds;
    public float[] Floats;
    public int[] Ints;

    public FSplitgate2FastGeoComponent(FFastGeoArchive Ar) : base(Ar)
    {
        Vector = new FVector(Ar);
        Ar.Position += 4;
        Material = Ar.ReadFPackageIndex();
        Transform = new FTransform(Ar);
        Bounds = new FBoxSphereBounds(Ar);
        Floats = Ar.ReadArray<float>(10);
        Ints = Ar.ReadArray<int>(4);
    }
}
