using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FMorphTargetVertexInfo
    {
        public readonly uint DataOffset;
        public readonly uint IndexBits;
        public readonly uint IndexMin;
        public readonly FVector PositionMin;
        public readonly uint TangentZBits;
        public readonly FVector TangentZMin;

        /// <summary>
        /// https://github.com/EpicGames/UnrealEngine/blob/ue5-main/Engine/Source/Runtime/Engine/Private/Rendering/MorphTargetVertexInfoBuffers.cpp#L459
        /// </summary>
        /// <param name="Ar"></param>
        public FMorphTargetVertexInfo(FArchive Ar)
        {
            DataOffset = Ar.Read<uint>();
            IndexBits = Ar.Read<uint>();
            IndexMin = Ar.Read<uint>();

            // MorphData.Add(BatchHeader.IndexBits |
            //               (BatchHeader.PositionBits.X << 5) | (BatchHeader.PositionBits.Y << 10) | (BatchHeader.PositionBits.Z << 15) |
            //               (BatchHeader.bTangents ? (1u << 20) : 0u) |
            //               (BatchHeader.NumElements << 21));


            var PositionMinX = Ar.Read<uint>();
            var PositionMinY = Ar.Read<uint>();
            var PositionMinZ = Ar.Read<uint>();
            TangentZBits = Ar.Read<uint>();
            var TangentZMinX = Ar.Read<uint>();
            var TangentZMinY = Ar.Read<uint>();
            var TangentZMinZ = Ar.Read<uint>();
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
            MorphData = packed.ReadArray(NumTotalBatches, () => new FMorphTargetVertexInfo(packed));

            MinimumValuePerMorph = Ar.ReadArray<FVector4>();
            MaximumValuePerMorph = Ar.ReadArray<FVector4>();
            BatchStartOffsetPerMorph = Ar.ReadArray<uint>();
            BatchesPerMorph = Ar.ReadArray<uint>();
            NumTotalBatches = Ar.Read<int>();
            PositionPrecision = Ar.Read<float>();
            TangentZPrecision = Ar.Read<float>();
        }
    }
}
