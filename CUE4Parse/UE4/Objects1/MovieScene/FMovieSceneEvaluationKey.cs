using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FMovieSceneEvaluationKey : IUStruct
    {
        /** ID of the sequence that the entity is contained within */
        public readonly FMovieSceneSequenceID SequenceID;
        /** ID of the track this key relates to */
        public readonly FMovieSceneTrackIdentifier TrackIdentifier;
        /** Index of the section template within the track this key relates to (or -1 where this key relates to a track) */
        public readonly uint SectionIndex;
    }
}
