using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class HashDataNode
{
    public readonly FHashData[] HashData = [];

    public HashDataNode(BinaryReader Ar)
    {
        HashData = FModReader.ReadElemListImp<FHashData>(Ar);
    }
}
