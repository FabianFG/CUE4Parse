using System.IO;
using CUE4Parse.UE4.FMod.Enums;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FQuantization
{
    public readonly EQuantizationUnit Unit;
    public readonly int Multiplier;

    public FQuantization(BinaryReader Ar)
    {
        Unit = (EQuantizationUnit) Ar.ReadUInt32();

        if (Unit <= EQuantizationUnit.EighthNote)
        {
            Multiplier = Ar.ReadInt32();
        }
        else
        {
            Multiplier = 0;
        }
    }
}
