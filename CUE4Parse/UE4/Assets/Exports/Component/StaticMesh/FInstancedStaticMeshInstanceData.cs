using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Component.StaticMesh
{
    public class FInstancedStaticMeshInstanceData
    {
        public readonly FMatrix Transform;

        public FInstancedStaticMeshInstanceData(FArchive Ar)
        {
            Transform = new FMatrix(Ar);
        }
    }
}