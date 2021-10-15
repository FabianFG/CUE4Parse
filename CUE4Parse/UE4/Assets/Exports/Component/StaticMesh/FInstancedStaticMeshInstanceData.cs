using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Component.StaticMesh
{
    public class FInstancedStaticMeshInstanceData
    {
        private readonly FMatrix Transform; // no need to expose raw matrix anymore

        // TODO: replicate the way UE handles this data, until then this should work better than a matrix I suppose
        public readonly FVector OffsetLocation;
        public readonly FRotator RelativeRotation;
        public readonly FVector RelativeScale3D;

        public FInstancedStaticMeshInstanceData(FArchive Ar)
        {
            Transform = new FMatrix(Ar);

            OffsetLocation = Transform.GetOrigin();
            RelativeRotation = Transform.Rotator();
            RelativeScale3D = Transform.GetScale();
        }
    }
}