using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.MovieScene;

public struct FMovieSceneSubSectionFieldData(FAssetArchive Ar) : IUStruct
{
    public TMovieSceneEvaluationTree<FMovieSceneSubSectionData> Field = new(Ar, () => new FMovieSceneSubSectionData(Ar));
}

public struct FMovieSceneSubSectionData(FAssetArchive Ar)
{
    /// <summary> The sub section itself  </summary>
    public FPackageIndex Section = new(Ar);
    /// <summary> The object binding that the sub section belongs to (usually zero) </summary>
    public FGuid ObjectBindingId = Ar.Read<FGuid>();
    /// <summary> Evaluation flags for the section </summary>
    public ESectionEvaluationFlags Flags = Ar.Read<ESectionEvaluationFlags>();
}
