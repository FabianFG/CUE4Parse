using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkChildren
{
    public uint[] ChildIds { get; }

    public AkChildren(FArchive Ar)
    {
        var numChildren = Ar.Read<uint>();
        ChildIds = new uint[numChildren];
        for (var i = 0; i < numChildren; i++)
        {
            ChildIds[i] = Ar.Read<uint>();
        }
    }
}
