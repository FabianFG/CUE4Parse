using System;
using CUE4Parse_Conversion.ActorX;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.PSK;

public class VSocket
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
        Ar.Write(SocketName.Substring(0, Math.Min(SocketName.Length, 64)), 64);
        Ar.Write(BoneName.Substring(0, Math.Min(BoneName.Length, 64)), 64);
        RelativeLocation.Serialize(Ar);
        RelativeRotation.Serialize(Ar);
        RelativeScale.Serialize(Ar);
    }
}
