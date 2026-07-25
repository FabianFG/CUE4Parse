using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Chaos;

public class TAABB<T> where T : struct
{
    public TVector<T> MMin;
    public TVector<T> MMax;
    
    public TAABB(FChaosArchive Ar, int dimensions)
    {
        MMin = new TVector<T>(Ar, dimensions);
        MMax = new TVector<T>(Ar, dimensions);
    }
}