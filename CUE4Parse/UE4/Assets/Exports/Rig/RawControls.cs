using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawControls
{
    public ushort PSDCount;
    public RawConditionalTable Conditionals;
    public RawPSDMatrix PSDs;

    public RawControls(FArchiveBigEndian Ar)
    {
        PSDCount = Ar.Read<ushort>();
        Conditionals = new RawConditionalTable(Ar);
        PSDs = new RawPSDMatrix(Ar);
    }
}
