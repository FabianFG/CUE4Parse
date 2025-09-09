using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawVector3Vector
{
    public float[] XS;
    public float[] YS;
    public float[] ZS;

    public RawVector3Vector(FArchiveBigEndian Ar)
    {
        XS = Ar.ReadArray<float>();
        YS = Ar.ReadArray<float>();
        ZS = Ar.ReadArray<float>();
    }
}
