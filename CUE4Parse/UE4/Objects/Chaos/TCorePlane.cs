using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Chaos;

[StructLayout(LayoutKind.Sequential)]
public struct TCorePlane<T> where T : struct
{
    public TVector<T> MX;
    public TVector<T> MNormal;

    public TCorePlane(FChaosArchive Ar, int dimensions)
    {
         MX = new TVector<T>(Ar, dimensions);
         MNormal = new TVector<T>(Ar, dimensions);
    }
}