using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.PCG;

public struct FPCGPoint : IUStruct
{
    public FTransform Transform;
    public float Density = 1.0f;
    public FVector BoundsMin = -FVector.OneVector;
    public FVector BoundsMax = FVector.OneVector;
    public FVector4 Color = FVector4.OneVector;
    public float Steepness = 0.5f;
    public int Seed = 0;
    public long MetadataEntry = -1;

    public FPCGPoint(FAssetArchive Ar)
    {
        var SerializeMask = Ar.Read<EPCGPointSerializeFields>();
        Transform = new FTransform(Ar);
        if (SerializeMask.HasFlag(EPCGPointSerializeFields.Density))
        {
            Density = Ar.Read<float>();
        }

        if (SerializeMask.HasFlag(EPCGPointSerializeFields.BoundsMin))
        {
            BoundsMin = new FVector(Ar);
        }

        if (SerializeMask.HasFlag(EPCGPointSerializeFields.BoundsMax))
        {
            BoundsMax = new FVector(Ar);
        }

        if (SerializeMask.HasFlag(EPCGPointSerializeFields.Color))
        {
            Color = new FVector4(Ar);
        }

        if (SerializeMask.HasFlag(EPCGPointSerializeFields.Steepness))
        {
            Steepness = Ar.Read<float>();
        }

        if (SerializeMask.HasFlag(EPCGPointSerializeFields.Seed))
        {
            Seed = Ar.Read<int>();
        }

        if (SerializeMask.HasFlag(EPCGPointSerializeFields.MetadataEntry))
        {
            MetadataEntry = Ar.Read<long>();
        }
    }

    [Flags]
    private enum EPCGPointSerializeFields : byte
    {
        None = 0,
        Density = 1 << 0,
        BoundsMin = 1 << 1,
        BoundsMax = 1 << 2,
        Color = 1 << 3,
        Steepness = 1 << 4,
        Seed = 1 << 5,
        MetadataEntry = 1 << 6
    }
}
