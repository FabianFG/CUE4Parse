using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FMeshSocket
{
    public FName SocketName;
    public FName BoneName;
    public FVector RelativeLocation;
    public FRotator RelativeRotation;
    public FVector RelativeScale;
    public bool bForceAlwaysAnimated;

    public FMeshSocket(FMutableArchive Ar)
    {
        SocketName = Ar.ReadFName();
        BoneName = Ar.ReadFName();
        RelativeLocation = new FVector(Ar);
        RelativeRotation = new FRotator(Ar);
        RelativeScale = new FVector(Ar);
        bForceAlwaysAnimated = Ar.ReadFlag();
    }
}
