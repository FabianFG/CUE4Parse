using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Writers.ActorX;

public readonly struct VSocket
{
    public readonly string SocketName;
    public readonly string BoneName;
    public readonly FVector RelativeLocation;
    public readonly FRotator RelativeRotation;
    public readonly FVector RelativeScale;

    public VSocket(string socketName, string boneName, FVector relativeLocation, FRotator relativeRotation, FVector relativeScale)
    {
        SocketName = socketName;
        BoneName = boneName;
        RelativeLocation = relativeLocation;
        RelativeRotation = relativeRotation;
        RelativeScale = relativeScale;
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(SocketName, 64);
        Ar.Write(BoneName, 64);
        RelativeLocation.Serialize(Ar);
        RelativeRotation.Serialize(Ar);
        RelativeScale.Serialize(Ar);
    }
}
