using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FMeshToMeshVertData
    {
        public readonly FVector4 PositionBaryCoordsAndDist;
        public readonly FVector4 NormalBaryCoordsAndDist;
        public readonly FVector4 TangentBaryCoordsAndDist;
        public readonly short[] SourceMeshVertIndices;
        public readonly float Weight;
        public readonly uint Padding;

        public FMeshToMeshVertData(FArchive Ar)
        {
            PositionBaryCoordsAndDist = Ar.Read<FVector4>();
            NormalBaryCoordsAndDist = Ar.Read<FVector4>();
            TangentBaryCoordsAndDist = Ar.Read<FVector4>();
            SourceMeshVertIndices = Ar.ReadArray<short>(4);

            if (FReleaseObjectVersion.Get(Ar) < FReleaseObjectVersion.Type.WeightFMeshToMeshVertData)
            {
                // Old version had "uint32 Padding[2]"
                var discard = Ar.Read<uint>();
                Padding = Ar.Read<uint>();
            }
            else
            {
                // New version has "float Weight and "uint32 Padding"
                Weight = Ar.Read<float>();
                Padding = Ar.Read<uint>();
            }
        }
    }
}
