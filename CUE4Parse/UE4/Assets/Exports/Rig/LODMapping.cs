using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class LODMapping
{
    public ushort[] LODs;
    public ushort[][] Indices;

    public LODMapping(FArchiveBigEndian Ar)
    {
        LODs = Ar.ReadArray<ushort>();
        Indices = Ar.ReadArray(Ar.ReadArray<ushort>);
    }
}
