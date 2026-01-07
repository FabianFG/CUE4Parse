using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics.Bodies;

public class FBodyShape
{
    public string Name;
    public uint Flags;

    public FBodyShape(FMutableArchive Ar)
    {
        Name = Ar.ReadFString();
        Flags = Ar.Read<uint>();
    }
}