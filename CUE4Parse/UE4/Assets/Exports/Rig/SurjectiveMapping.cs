using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class SurjectiveMapping
{
    public ushort[] From;
    public ushort[] To;

    public SurjectiveMapping(FArchiveBigEndian Ar)
    {
        From = Ar.ReadArray<ushort>();
        To = Ar.ReadArray<ushort>();
    }
}
