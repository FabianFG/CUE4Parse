using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawAnimatedMaps
{
    public ushort[] LODs;
    public RawConditionalTable Conditionals;

    public RawAnimatedMaps(FArchiveBigEndian Ar)
    {
        LODs = Ar.ReadArray<ushort>();
        Conditionals = new RawConditionalTable(Ar);
    }
}
