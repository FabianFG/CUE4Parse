using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawConditionalTable
{
    public ushort[] InputIndices;
    public ushort[] OutputIndices;
    public float[] FromValues;
    public float[] ToValues;
    public float[] SlopeValues;
    public float[] CutValues;

    public RawConditionalTable(FArchiveBigEndian Ar)
    {
        InputIndices = Ar.ReadArray<ushort>();
        OutputIndices = Ar.ReadArray<ushort>();
        FromValues = Ar.ReadArray<float>();
        ToValues = Ar.ReadArray<float>();
        SlopeValues = Ar.ReadArray<float>();
        CutValues = Ar.ReadArray<float>();
    }
}
