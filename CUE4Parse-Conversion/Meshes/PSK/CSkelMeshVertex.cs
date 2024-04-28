using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CSkelMeshVertex : CMeshVertex
    {
        private readonly List<BoneInfluence> _influences = [];
        
        public CSkelMeshVertex(FVector position, FPackedNormal normal, FPackedNormal tangent, FMeshUVFloat uv) : base(position, normal, tangent, uv)
        {
        }

        public IReadOnlyList<BoneInfluence> Influences => _influences;

        public void AddInfluence(short bone, byte weight)
        {
            _influences.Add(new BoneInfluence(bone, weight));
        }
    }
}