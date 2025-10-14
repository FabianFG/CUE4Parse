using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FFadeCurve
{
    public readonly FModGuid BusGuid;
    public readonly FModGuid CurveGuid;

    public FFadeCurve(BinaryReader Ar)
    {
        BusGuid = new FModGuid(Ar);
        CurveGuid = new FModGuid(Ar);
    }
}
