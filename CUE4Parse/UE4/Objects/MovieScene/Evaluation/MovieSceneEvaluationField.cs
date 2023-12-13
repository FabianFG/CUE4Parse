using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.MovieScene.Evaluation;

public class FMovieSceneEvaluationFieldEntityTree : IUStruct, ISerializable
{
    public TMovieSceneEvaluationTree<FEntityAndMetaDataIndex> SerializedData;

    public FMovieSceneEvaluationFieldEntityTree(FArchive Ar)
    {
        SerializedData = new TMovieSceneEvaluationTree<FEntityAndMetaDataIndex>(Ar);
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(SerializedData);
    }
}

public struct FEntityAndMetaDataIndex : ISerializable
{
    public int EntityIndex;
    public int MetaDataIndex;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(EntityIndex);
        Ar.Write(MetaDataIndex);
    }

    public bool Equals(FEntityAndMetaDataIndex other) => EntityIndex == other.EntityIndex && MetaDataIndex == other.MetaDataIndex;
    public override bool Equals(object? obj) => obj is FEntityAndMetaDataIndex other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(EntityIndex, MetaDataIndex);
    public static bool operator ==(FEntityAndMetaDataIndex left, FEntityAndMetaDataIndex right) => left.Equals(right);
    public static bool operator !=(FEntityAndMetaDataIndex left, FEntityAndMetaDataIndex right) => !(left == right);
}