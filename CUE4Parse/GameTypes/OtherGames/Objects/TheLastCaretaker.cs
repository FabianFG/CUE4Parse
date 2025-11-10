using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public struct VoyagePackedLocalTransform : IUStruct
{
    public FVector Location;
    public FQuat Rotation;
    public FVector Scale;
    public VoyagePackedLocalTransform(FAssetArchive Ar)
    {
        Location = Ar.Read<FVector>();
        var rot = Ar.Read<FHalfVector>();
        Rotation = new FRotator((float) rot.X, (float) rot.Y, (float) rot.Z).Quaternion();
        Scale = Ar.Read<FHalfVector>();
    }
}
