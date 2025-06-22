using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Animation.PoseSearch;

public class FSearchIndexBase
{
    public float[] Values;
    public FSparsePoseMultiMap<int> ValuesVectorToPoseIndexes;
    public FPoseMetadata[] PoseMetadata;
    public bool bAnyBlockTransition;
    public FSearchIndexAsset[] Assets;
    // Experimental, this feature might be removed without warning, not for production use
    public FEventData EventData;
    public float MinCostAddend;
    public FSearchStats Stats;

    public FSearchIndexBase(FAssetArchive Ar)
    {
        Values = Ar.ReadArray<float>();
        ValuesVectorToPoseIndexes = new FSparsePoseMultiMap<int>(Ar);
        PoseMetadata = Ar.ReadArray(() => new FPoseMetadata(Ar));
        bAnyBlockTransition = Ar.ReadBoolean();
        Assets = Ar.ReadArray(() => new FSearchIndexAsset(Ar));
        if (Ar.Game >= EGame.GAME_UE5_6)
        {
            EventData = new FEventData(Ar);
        }
        MinCostAddend = Ar.Read<float>();
        Stats = Ar.Read<FSearchStats>();
    }
}
