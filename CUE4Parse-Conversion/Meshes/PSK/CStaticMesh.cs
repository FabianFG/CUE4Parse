using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Meshes.PSK;

/// <summary>
/// TODO: this needs a refactor
/// </summary>
public class CStaticMesh : CMesh<CStaticMeshLod>
{
    public FPackageIndex? BodySetup { get; init; }
    public FPackageIndex[]? Sockets { get; init; }

    public CStaticMesh(FBox box, FSphere sphere) : base(box, sphere)
    {

    }

    public CStaticMesh(FBoxSphereBounds bounds) : base(bounds)
    {

    }
}
