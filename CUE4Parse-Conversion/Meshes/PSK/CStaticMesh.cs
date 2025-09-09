using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CStaticMesh : IDisposable
    {
        public readonly List<CStaticMeshLod> LODs = [];
        
        public FBox BoundingBox;
        public FSphere BoundingSphere;

        public void FinalizeMesh()
        {
            foreach (var levelOfDetail in LODs)
            {
                levelOfDetail?.BuildNormals();
            }
        }

        public void Dispose()
        {
            foreach (var lod in LODs)
                lod.Dispose();
            
            LODs.Clear();
        }
    }
}
