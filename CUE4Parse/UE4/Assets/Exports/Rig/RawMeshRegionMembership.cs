using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawMeshRegionMembership
{
    public string[][] RegionNames;
    public ushort[][][] Indices;

    public RawMeshRegionMembership(FArchiveBigEndian Ar)
    {
        RegionNames = Ar.ReadArray(() => Ar.ReadArray(Ar.ReadString));
        Indices = Ar.ReadArray(() => Ar.ReadArray(Ar.ReadArray<ushort>));
    }
}
