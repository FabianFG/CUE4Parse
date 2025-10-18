using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FLegacyParameterConditions
{
    public readonly FModGuid BaseGuid;
    public readonly float Minimum;
    public readonly float Maximum;

    public FLegacyParameterConditions(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        Minimum = Ar.ReadSingle();
        Maximum = Ar.ReadSingle();
    }
}
