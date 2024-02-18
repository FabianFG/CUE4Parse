using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class FCustomizableObjectMeshToMeshVertData
{
    public float[] PositionBaryCoordsAndDist = new float[4];
    public float[] NormalBaryCoordsAndDist = new float[4];
    public float[] TangentBaryCoordsAndDist = new float[4];
    public ushort[] SourceMeshVertIndices = new ushort[4];
    public float Weight;
    public short SourceAssetIndex;
    public short SourceAssetLodIndex;
    
    public FCustomizableObjectMeshToMeshVertData(FArchive Ar)
    {
        for (var i = 0; i < PositionBaryCoordsAndDist.Length; i++)
        {
            PositionBaryCoordsAndDist[i] = Ar.Read<float>();
        }

        for (var i = 0; i < NormalBaryCoordsAndDist.Length; i++)
        {
            NormalBaryCoordsAndDist[i] = Ar.Read<float>();
        }

        for (var i = 0; i < TangentBaryCoordsAndDist.Length; i++)
        {
            TangentBaryCoordsAndDist[i] = Ar.Read<float>();
        }

        for (var i = 0; i < SourceMeshVertIndices.Length; i++)
        {
            SourceMeshVertIndices[i] = Ar.Read<ushort>();
        }

        Weight = Ar.Read<float>();
        SourceAssetIndex = Ar.Read<short>();
        SourceAssetLodIndex = Ar.Read<short>();
    }
}