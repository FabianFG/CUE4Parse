using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FUserPropertyString
{
    public readonly string Key;
    public readonly string Value;

    public FUserPropertyString(BinaryReader Ar)
    {
        Key = FModReader.ReadString(Ar);
        Value = FModReader.ReadString(Ar);
    }
}
