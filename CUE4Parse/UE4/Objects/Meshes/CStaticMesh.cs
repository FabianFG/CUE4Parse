using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Meshes
{
    public class CStaticMesh
    {
        private readonly FBoxSphereBounds _bounds = new (new FVector(0f, 0f, 0f), new FVector(0f, 0f, 0f), 0f);
        
        public readonly UStaticMesh OriginalMesh;
        public readonly FBox BoundingBox;
        public readonly FSphere BoundingSphere;
        public readonly List<CStaticMeshLod> LODs;
        
        public CStaticMesh(UStaticMesh originalMesh, List<CStaticMeshLod> lods)
        {
            OriginalMesh = originalMesh;
            BoundingBox = new FBox(_bounds.Origin - _bounds.BoxExtend, _bounds.Origin + _bounds.BoxExtend);
            BoundingSphere = new FSphere(0f, 0f, 0f, _bounds.SphereRadius / 2);
            LODs = lods;
        }
        
        public void FinalizeMesh()
        {
            foreach (var levelOfDetail in LODs)
            {
                levelOfDetail.BuildNormals();
            }
        }
    }
}