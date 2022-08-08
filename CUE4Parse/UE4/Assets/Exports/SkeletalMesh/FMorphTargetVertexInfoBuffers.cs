using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FMorphTargetVertexInfoBuffers
    {
        public readonly uint[] MorphData;
        public readonly FVector4[] MinimumValuePerMorph;
        public readonly FVector4[] MaximumValuePerMorph;
        public readonly uint[] BatchStartOffsetPerMorph;
        public readonly uint[] BatchesPerMorph;
        public readonly uint NumTotalBatches;
        public readonly float PositionPrecision;
        public readonly float TangentZPrecision;

        public FMorphTargetVertexInfoBuffers(FArchive Ar)
        {
            MorphData = Ar.ReadArray<uint>();
            MinimumValuePerMorph = Ar.ReadArray<FVector4>();
            MaximumValuePerMorph = Ar.ReadArray<FVector4>();
            BatchStartOffsetPerMorph = Ar.ReadArray<uint>();
            BatchesPerMorph = Ar.ReadArray<uint>();
            NumTotalBatches = Ar.Read<uint>();
            PositionPrecision = Ar.Read<float>();
            TangentZPrecision = Ar.Read<float>();
        }
    }
}
