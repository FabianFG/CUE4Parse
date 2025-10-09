using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes;

public class FormatInfo
{
    public readonly int FileVersion;
    public readonly int CompatVersion;

    public FormatInfo(BinaryReader Ar)
    {
        FileVersion = Ar.ReadInt32();
        CompatVersion = Ar.ReadInt32();
    }
}
