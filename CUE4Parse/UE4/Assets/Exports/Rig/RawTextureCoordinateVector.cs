using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawTextureCoordinateVector
{
    public float[] Us;
    public float[] Vs;

    public RawTextureCoordinateVector(FArchiveBigEndian Ar)
    {
        Us = Ar.ReadArray<float>();
        Vs = Ar.ReadArray<float>();
    }
}
