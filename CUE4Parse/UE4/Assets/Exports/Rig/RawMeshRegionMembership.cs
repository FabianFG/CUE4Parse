using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawMeshRegionMembership(FArchiveBigEndian Ar)
{
    public string[][] RegionNames = Ar.ReadArray(() => Ar.ReadArray(Ar.ReadString));
    public ushort[][][] Indices = Ar.ReadArray(() => Ar.ReadArray(Ar.ReadArray<ushort>));
}
