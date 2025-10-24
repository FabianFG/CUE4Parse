using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FParameterId
{
    public readonly uint Data1;
    public readonly uint Data2;

    public FParameterId(BinaryReader Ar)
    {
        Data1 = Ar.ReadUInt32();
        Data2 = Ar.ReadUInt32();
    }
}
