using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Engine;

public class FConvexVolume : IUStruct
{
    public FPlane[] Planes;
    public FPlane[] PermutedPlanes;

    public FConvexVolume(FAssetArchive Ar)
    {
        Planes = Ar.ReadArray(() => new FPlane(Ar));
        PermutedPlanes = Ar.ReadArray(() => new FPlane(Ar));
    }
}
