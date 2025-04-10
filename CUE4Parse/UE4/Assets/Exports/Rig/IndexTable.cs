using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class IndexTable
{
    public Index[] Entries;

    public IndexTable(FArchiveBigEndian Ar)
    {
        Entries = Ar.ReadArray(() => new Index(Ar));
    }
}
