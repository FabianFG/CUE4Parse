using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Component.StaticMesh
{
    public class FInstancedStaticMeshInstanceData
    {
        private readonly FMatrix Transform; // don't expose the raw matrix for now

        public FTransform TransformData;

        public FInstancedStaticMeshInstanceData(FArchive Ar)
        {
            Transform = new FMatrix(Ar);

            TransformData.SetFromMatrix(Transform);
        }
    }
}
