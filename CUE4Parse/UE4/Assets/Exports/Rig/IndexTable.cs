using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class IndexTable
{
    public Index[] Entries;

    public IndexTable(FArchiveBigEndian Ar)
    {
        Entries = Ar.ReadArray(() => new Index(Ar));
    }

    public IndexTable(SectionLookupTable slt, DNAVersion version)
    {
        Entries =
        [
            new Index("desc", version, slt.Descriptor, 0),
            new Index("defn", version, slt.Definition, 0),
            new Index("bhvr", version, slt.Behaviour, 0),
            new Index("geom", version, slt.Geometry, 0),
        ];
    }
}
