using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.PCG;

public class FPCGPointArray : IUStruct
{
    public FPCGPointArrayProperty<FTransform> Transform;
    public FPCGPointArrayProperty<float> Density;
    public FPCGPointArrayProperty<FVector> BoundsMin;
    public FPCGPointArrayProperty<FVector> BoundsMax;
    public FPCGPointArrayProperty<FVector4> Color;
    public FPCGPointArrayProperty<float> Steepness;
    public FPCGPointArrayProperty<int> Seed;
    public FPCGPointArrayProperty<long> MetadataEntry;
    public int NumPoints = 0;

    public FPCGPointArray(FAssetArchive Ar)
    {
        NumPoints = Ar.Read<int>();
        Transform = new FPCGPointArrayProperty<FTransform>(Ar, () => new FTransform(Ar));
        Density = new FPCGPointArrayProperty<float>(Ar);
        BoundsMin = new FPCGPointArrayProperty<FVector>(Ar, () => new FVector(Ar));
        BoundsMax = new FPCGPointArrayProperty<FVector>(Ar, () => new FVector(Ar));
        Color = new FPCGPointArrayProperty<FVector4>(Ar, () => new FVector4(Ar));
        Steepness = new FPCGPointArrayProperty<float>(Ar);
        Seed = new FPCGPointArrayProperty<int>(Ar);
        MetadataEntry = new FPCGPointArrayProperty<long>(Ar);
    }
}
