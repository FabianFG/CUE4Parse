using CUE4Parse.UE4.Assets.Readers;

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

    public float[] DeviationEditorOnly;
    public float PCAExplainedVarianceEditorOnly;

    public FSearchIndex(FAssetArchive Ar) : base(Ar)
    {
        WeightsSqrt = Ar.ReadArray<float>();
        PCAValues = Ar.ReadArray<float>();
        PCAValuesVectorToPoseIndexes = new FSparsePoseMultiMap<int>(Ar);
        PCAProjectionMatrix = Ar.ReadArray<float>();
        Mean = Ar.ReadArray<float>();
        if (Ar.Game < GAME_UE5_6)
        {
            PCAExplainedVariance = Ar.Read<float>();
        }
        VPTree = new FVPTree(Ar);
        KDTree = new FKDTree(Ar);
        if (Ar.Game is >= GAME_UE5_6 and < GAME_UE5_8)
        {
            PCAExplainedVariance = Ar.Read<float>();
        }

        if (Ar.Game >= GAME_UE5_6 && !Ar.IsFilterEditorOnly)
        {
            DeviationEditorOnly = Ar.ReadArray<float>();
            PCAExplainedVarianceEditorOnly = Ar.Read<float>();
        }
    }
}
