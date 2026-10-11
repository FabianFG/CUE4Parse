using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.TransformersFallofCybertron.Objects;

public struct FTRMeshUnkStream
{
    public int ItemSize;
    public int NumVerts;
    public int[] Data;

    public FTRMeshUnkStream(FArchive Ar)
    {
        ItemSize = Ar.Read<int>();
        NumVerts = Ar.Read<int>();

        if (ItemSize == 0 || NumVerts == 0)
        {
            Data = [];
            return;
        }

        Data = Ar.ReadBulkArray<int>();
    }

    public FTRMeshUnkStream(int[] data)
    {
        Data = data;
        ItemSize = 4;
        NumVerts = data.Length;
    }
}
