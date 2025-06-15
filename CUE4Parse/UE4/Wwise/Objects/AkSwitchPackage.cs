using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkSwitchPackage
{
    public readonly uint SwitchId;
    public readonly List<uint> NodeIds;

    public AkSwitchPackage(FArchive Ar)
    {
        SwitchId = Ar.Read<uint>();
        var numItems = Ar.Read<uint>();
        NodeIds = [];
        for (var i = 0; i < numItems; i++)
        {
            NodeIds.Add(Ar.Read<uint>());
        }
    }
}
