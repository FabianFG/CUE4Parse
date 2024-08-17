using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.MovieScene;

public struct FMovieSceneSubSequenceTreeEntry : IUStruct
{
    public uint SequenceID;
    public ESectionEvaluationFlags Flags;
    public FStructFallback? RootToSequenceWarpCounter;

    public FMovieSceneSubSequenceTreeEntry(FAssetArchive Ar)
    {
        SequenceID = Ar.Read<uint>();
        Flags = (ESectionEvaluationFlags) Ar.ReadByte();
        if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.AddedSubSequenceEntryWarpCounter ||
            FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.AddedSubSequenceEntryWarpCounter)
        {
            RootToSequenceWarpCounter = new FStructFallback(Ar, "MovieSceneWarpCounter");
        }
    }
}
