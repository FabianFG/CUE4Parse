using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh;

public class FMeshMorph
{
    public FName[] Names;
    public FVector4[] MaximumValuePerMorph;
    public FVector4[] MinimumValuePerMorph;
    public uint[] BatchStartOffsetPerMorph;
    public uint[] BatchesPerMorph;
    public int[][] SurfacesInUsePerMorph;
    public EMorphUsageFlags[] UsageFlagsPerMorph;
    public uint NumTotalBatches;
    public float PositionPrecision;
    public float TangentZPrecision;

    public FMeshMorph(FMutableArchive Ar)
    {
        Names =  Ar.ReadArray(Ar.ReadFName);
        MaximumValuePerMorph = Ar.ReadArray<FVector4>();
        MinimumValuePerMorph = Ar.ReadArray<FVector4>();
        BatchStartOffsetPerMorph = Ar.ReadArray<uint>();
        BatchesPerMorph = Ar.ReadArray<uint>();
        SurfacesInUsePerMorph = Ar.ReadArray(Ar.ReadArray<int>);
        UsageFlagsPerMorph = Ar.ReadArray<EMorphUsageFlags>();
        NumTotalBatches = Ar.Read<uint>();
        PositionPrecision = Ar.Read<float>();
        TangentZPrecision = Ar.Read<float>();
    }

    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EMorphUsageFlags : byte
    {
        None     = 0,

        Baked    = 1 << 0,
        RealTime = 1 << 1,
        External = 1 << 2,

        Merged   = RealTime | External,

        AllFlags = 0xFF
    }
}
