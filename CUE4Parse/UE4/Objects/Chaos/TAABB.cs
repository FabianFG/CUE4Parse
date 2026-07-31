using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Chaos;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TAABB<T> where T : struct
{
    public readonly TVector<T> MMin;
    public readonly TVector<T> MMax;

    public TAABB(FChaosArchive Ar, int dimensions)
    {
        MMin = new TVector<T>(Ar, dimensions);
        MMax = new TVector<T>(Ar, dimensions);
    }

    public TAABB(int dimension, T initialValue)
    {
        MMin = new TVector<T>(dimension, initialValue);
        MMax = new TVector<T>(dimension, initialValue);
    }
}
