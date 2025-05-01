using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class CAkChildren
{
    public uint[] ChildIDs { get; }

    public CAkChildren(FArchive Ar)
    {
        var numChildren = Ar.Read<uint>();
        ChildIDs = new uint[numChildren];
        for (var i = 0; i < numChildren; i++)
        {
            ChildIDs[i] = Ar.Read<uint>();
        }
    }
}
