using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class ControllerOwnerNode
{
    public readonly FModGuid[] Controllers;

    public ControllerOwnerNode(BinaryReader Ar)
    {
        Controllers = FModReader.ReadElemListImp<FModGuid>(Ar);
    }
}
