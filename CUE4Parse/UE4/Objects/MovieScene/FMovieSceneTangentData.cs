using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.MovieScene;

public readonly struct FMovieSceneTangentData : IUStruct
{
    public readonly float ArriveTangent;
    public readonly float LeaveTangent;
    public readonly float ArriveTangentWeight;
    public readonly float LeaveTangentWeight;
    public readonly ERichCurveTangentWeightMode TangentWeightMode;

    public FMovieSceneTangentData(FAssetArchive Ar)
    {
        ArriveTangent = Ar.Read<float>();
        LeaveTangent = Ar.Read<float>();
        if (FSequencerObjectVersion.Get(Ar) < FSequencerObjectVersion.Type.SerializeFloatChannelCompletely)
        {
            TangentWeightMode = Ar.Read<ERichCurveTangentWeightMode>();
            ArriveTangentWeight = Ar.Read<float>();
            LeaveTangentWeight = Ar.Read<float>();
        }
        else
        {
            ArriveTangentWeight = Ar.Read<float>();
            LeaveTangentWeight = Ar.Read<float>();
            TangentWeightMode = Ar.Read<ERichCurveTangentWeightMode>();
            Ar.Position += 3; // Padding
        }
    }
}
