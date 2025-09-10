using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

[StructLayout(LayoutKind.Sequential)]
public struct FDeltaBatchHeader
{
    public uint DataOffset;
    public uint NumElements;
    public bool bTangents;
    public sbyte IndexBits;
    public TIntVector3<sbyte> PositionBits;
    public TIntVector3<sbyte> TangentZBits;
    public uint IndexMin;
    public FIntVector PositionMin;
    public FIntVector TangentZMin;

    public FDeltaBatchHeader(FAssetArchive Ar)
    {
        DataOffset = Ar.Read<uint>();
        NumElements = Ar.Read<uint>();
        bTangents = Ar.ReadBoolean();
        IndexBits = Ar.Read<sbyte>();
        PositionBits = Ar.Read<TIntVector3<sbyte>>();
        TangentZBits = Ar.Read<TIntVector3<sbyte>>();
        IndexMin = Ar.Read<uint>();
        PositionMin = Ar.Read<FIntVector>();
        TangentZMin = Ar.Read<FIntVector>();
    }
}