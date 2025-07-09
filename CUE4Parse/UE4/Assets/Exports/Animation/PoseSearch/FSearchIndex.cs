using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Animation.PoseSearch;

public class FSearchIndex : FSearchIndexBase
{
    public float[] WeightsSqrt;
    public float[] PCAValues;
    public FSparsePoseMultiMap<int> PCAValuesVectorToPoseIndexes;
    public float[] PCAProjectionMatrix;
    public float[] Mean;
    public float PCAExplainedVariance;
    public FVPTree VPTree;
    public FKDTree KDTree;

    public FSearchIndex(FAssetArchive Ar) : base(Ar)
    {
        WeightsSqrt = Ar.ReadArray<float>();
        PCAValues = Ar.ReadArray<float>();
        PCAValuesVectorToPoseIndexes = new FSparsePoseMultiMap<int>(Ar);
        PCAProjectionMatrix = Ar.ReadArray<float>();
        Mean = Ar.ReadArray<float>();
        if (Ar.Game < EGame.GAME_UE5_6)
        {
            PCAExplainedVariance = Ar.Read<float>();
        }
        VPTree = new FVPTree(Ar);
        KDTree = new FKDTree(Ar);
        if (Ar.Game >= EGame.GAME_UE5_6)
        {
            PCAExplainedVariance = Ar.Read<float>();
        }
    }
}
