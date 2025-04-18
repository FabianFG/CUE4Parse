using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawPSDMatrix
{
    public ushort[] Rows;
    public ushort[] Columns;
    public float[] Values;

    public RawPSDMatrix(FArchiveBigEndian Ar)
    {
        Rows = Ar.ReadArray<ushort>();
        Columns = Ar.ReadArray<ushort>();
        Values = Ar.ReadArray<float>();
    }
}
