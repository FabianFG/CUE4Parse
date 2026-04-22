using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;

namespace CUE4Parse_Conversion.Meshes.PSK;

public class CMeshVertex(FVector position, FPackedNormal normal, FPackedNormal tangent, FMeshUVFloat uv)
{
    public FVector Position = position;
    public FVector4 Normal = normal;
    public FVector4 Tangent = tangent;
    public FMeshUVFloat UV = uv;

    public CMeshVertex() : this(FVector.ZeroVector, new FPackedNormal(0), new FPackedNormal(0), new FMeshUVFloat(0, 0))
    {

    }
}
