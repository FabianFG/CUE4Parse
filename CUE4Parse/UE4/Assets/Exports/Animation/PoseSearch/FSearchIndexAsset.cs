using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Animation.PoseSearch;

public class FSearchIndexAsset(FAssetArchive Ar)
{
    public int SourceAssetIdx = Ar.Read<int>();
    public bool bMirrored = Ar.ReadBoolean();
    public bool bLooping = Ar.ReadBoolean();
    public bool bDisableReselection = Ar.ReadBoolean();
    public int PermutationIdx = Ar.Read<int>();
    public float BlendParameterX = Ar.Read<float>();
    public float BlendParameterY = Ar.Read<float>();
    public int FirstPoseIdx = Ar.Read<int>();
    public int FirstSampleIdx = Ar.Read<int>();
    public int LastSampleIdx = Ar.Read<int>();
    public float ToRealTimeFactor = Ar.Game >= EGame.GAME_UE5_6 ? Ar.Read<float>() : 1.0f;
}
