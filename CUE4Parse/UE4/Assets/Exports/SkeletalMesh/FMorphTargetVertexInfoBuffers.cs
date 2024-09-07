using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public struct FQuantizedDelta(FIntVector position, FIntVector tangent, uint index)
{
    public FIntVector Position = position;
    public FIntVector TangentZ = tangent;
    public uint Index = index;
}

public class FMorphTargetVertexInfo
{
    public uint DataOffset;
    public uint IndexBits;
    public uint IndexMin;
    public bool bTangents;
    public uint NumElements;
    public TIntVector3<uint> PositionBits;
    public TIntVector3<int> PositionMin;
    public TIntVector3<uint> TangentZBits;
    public TIntVector3<int> TangentZMin;
    [JsonIgnore]
    public FQuantizedDelta[] QuantizedDelta;

    public FMorphTargetVertexInfo(FArchive Ar)
    {
        DataOffset = Ar.Read<uint>();
        var bits = Ar.Read<uint>();
        IndexBits = bits & 0x1F;
        PositionBits = new TIntVector3<uint>(bits >> 5 & 0x1F, bits >> 10 & 0x1F, bits >> 15 & 0x1F);
        bTangents = (bits >> 20 & 0x1) == 1;
        NumElements = bits >> 21;
        IndexMin = Ar.Read<uint>();
        PositionMin = Ar.Read<TIntVector3<int>>();
        bits = Ar.Read<uint>();
        TangentZBits = new TIntVector3<uint>(bits & 0x1F, bits >> 5 & 0x1F, bits >> 10 & 0x1F);
        TangentZMin = Ar.Read<TIntVector3<int>>();

        var savedPos = Ar.Position;

        var numPositionBits = PositionBits.X + PositionBits.Y + PositionBits.Z;
        var numTangentBits = TangentZBits.X + TangentZBits.Y + TangentZBits.Z;
        var strideInBits = IndexBits + numPositionBits + (bTangents ? numTangentBits : 0);
        var size = (strideInBits * NumElements + 7) >> 3;

        Ar.Position = DataOffset;
        QuantizedDelta = new FQuantizedDelta[NumElements];
        using (var packed = new FBitArchive("PackedData", Ar.ReadBytes((int)size.Align(4))))
        {
            for (var i = 0; i < NumElements; i++)
            {
                uint vert = (uint) (IndexMin + i + packed.Read(IndexBits));
                QuantizedDelta[i] = new FQuantizedDelta(packed.ReadIntVector(PositionBits),
                    bTangents ? packed.ReadIntVector(TangentZBits) : FIntVector.Zero, vert);
            }
        }

        Ar.Position = savedPos;
    }
}

public class FMorphTargetVertexInfoBuffers
{
    [JsonIgnore]
    public readonly FMorphTargetVertexInfo[] MorphData;
    public readonly FVector4[] MinimumValuePerMorph;
    public readonly FVector4[] MaximumValuePerMorph;
    public readonly uint[] BatchStartOffsetPerMorph;
    public readonly uint[] BatchesPerMorph;
    public readonly int NumTotalBatches;
    public readonly float PositionPrecision;
    public readonly float TangentZPrecision;

    public FMorphTargetVertexInfoBuffers(FArchive Ar)
    {
        var packed = new FByteArchive("PackedMorphData", Ar.ReadArray<byte>(Ar.Read<int>() * sizeof(uint)), Ar.Versions);
        MinimumValuePerMorph = Ar.ReadArray<FVector4>();
        MaximumValuePerMorph = Ar.ReadArray<FVector4>();
        BatchStartOffsetPerMorph = Ar.ReadArray<uint>();
        BatchesPerMorph = Ar.ReadArray<uint>();
        NumTotalBatches = Ar.Read<int>();
        PositionPrecision = Ar.Read<float>();
        TangentZPrecision = Ar.Read<float>();
        MorphData = packed.ReadArray(NumTotalBatches, () => new FMorphTargetVertexInfo(packed));
    }
}
