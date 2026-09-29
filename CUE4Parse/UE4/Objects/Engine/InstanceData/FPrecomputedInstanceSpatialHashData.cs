using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Engine.InstanceData;

public class FPrecomputedInstanceSpatialHashData
{
    public FCompressedSpatialHashItem[] Hashes;
    public int[] ProxyIndexToComponentIndexRemap;
    public uint PrimitiveLocalToWorldHash = 0u;

    public FPrecomputedInstanceSpatialHashData(FArchive Ar)
    {
        PrimitiveLocalToWorldHash = Ar.Read<uint>();
        Hashes = Ar.ReadBulkArray(() => new FCompressedSpatialHashItem(Ar));
        ProxyIndexToComponentIndexRemap = Ar.ReadBulkArray<int>();
    }
}
