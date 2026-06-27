using System.Diagnostics;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TIntVector1<T>(T value) : IUStruct
{
    public readonly T Value = value;
}

[StructLayout(LayoutKind.Sequential)]
public struct TIntVector2<T> : IUStruct
{
    public T X;
    public T Y;

    public TIntVector2(T x, T y)
    {
        X = x;
        Y = y;
    }

    public override string ToString()
    {
        return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}";
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct TIntVector3<T> : IUStruct
{
    public readonly T X;
    public readonly T Y;
    public readonly T Z;

    public TIntVector3(T x, T y, T z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override string ToString()
    {
        return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}";
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct TIntVector4<T> : IUStruct
{
    public readonly T X;
    public readonly T Y;
    public readonly T Z;
    public readonly T W;

    public TIntVector4(T x, T y, T z, T w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public override string ToString()
    {
        return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}, {nameof(W)}: {W}";
    }
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class TVector<T> where T : struct
{
    private readonly T[] _values;

    public int Dimension => _values.Length;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => string.Join(", ", _values);

    public TVector(int dimension)
    {
        _values = new T[dimension];
    }

    public TVector(int dimension, T initialValue)
    {
        _values = new T[dimension];

        for (int i = 0; i < dimension; i++)
        {
            _values[i] = initialValue;
        }
    }

    // serializeReal vector.h
    public TVector(FArchive Ar, int dimension)
    {
        _values = new T[dimension];
        for (int i = 0; i < dimension; i++)
        {
            _values[i] = Ar.Read<T>();
        }
    }

    public TVector(params T[] values)
    {
        _values = new T[values.Length];
        Array.Copy(values, _values, values.Length);
    }

    public T this[int i] => _values[i];
}
