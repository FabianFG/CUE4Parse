using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Chaos;


public class TAABB<T> where T: struct
{
    public TVector<T> Min;
    public TVector<T> Max;

    public TAABB(int dimension, T initialValue)
    {
        Min = new TVector<T>(dimension, initialValue);
        Max = new TVector<T>(dimension, initialValue);
    }

    public TAABB(FArchive Ar, int dimension)
    {
        Min = new TVector<T>(Ar, dimension);
        Max = new TVector<T>(Ar, dimension);
    }

    // SerializeReal
    public void Serialize(FArchive Ar)
    {
        Min = new TVector<T>(Ar, Min.Dimension);
        Max = new TVector<T>(Ar, Max.Dimension);
    }
}


