using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FUserPropertyFloat
{
    public readonly string Name;
    public readonly float Value;

    public FUserPropertyFloat(BinaryReader Ar)
    {
        Name = FModReader.ReadString(Ar);
        Value = Ar.ReadSingle();
    }
}
