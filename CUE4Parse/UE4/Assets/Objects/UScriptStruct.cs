using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Ai;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.LevelSequence;
using CUE4Parse.UE4.Objects.MovieScene;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class UScriptStruct
    {
        public readonly IUStruct StructType;

        public UScriptStruct(FAssetArchive Ar, string? structName, ReadType type)
        {
            StructType = structName switch
            {
                "Box" => type == ReadType.ZERO ? new FBox() : Ar.Read<FBox>(),
                "Box2D" => type == ReadType.ZERO ? new FBox2D() : Ar.Read<FBox2D>(),
                "Color" => type == ReadType.ZERO ? new FColor() : Ar.Read<FColor>(),
                "IntPoint" => type == ReadType.ZERO ? new FIntPoint() : Ar.Read<FIntPoint>(),
                "IntVector" => type == ReadType.ZERO ? new FIntVector() : Ar.Read<FIntVector>(),
                "LinearColor" => type == ReadType.ZERO ? new FLinearColor() : Ar.Read<FLinearColor>(),
                "Quat" => type == ReadType.ZERO ? new FQuat() : Ar.Read<FQuat>(),
                "Rotator" => type == ReadType.ZERO ? new FRotator() : Ar.Read<FRotator>(),
                "Vector" => type == ReadType.ZERO ? new FVector() : Ar.Read<FVector>(),
                "Vector2D" => type == ReadType.ZERO ? new FVector2D() : Ar.Read<FVector2D>(),
                "Vector4" => type == ReadType.ZERO ? new FVector4() : Ar.Read<FVector4>(),
                "DateTime" => type == ReadType.ZERO ? new FDateTime() : Ar.Read<FDateTime>(),
                "Timespan" => type == ReadType.ZERO ? new FDateTime() : Ar.Read<FDateTime>(),
                "FrameNumber" => type == ReadType.ZERO ? new FFrameNumber() : Ar.Read<FFrameNumber>(),
                "FrameRate" => type == ReadType.ZERO ? new FFrameRate() : Ar.Read<FFrameRate>(),
                "Guid" => type == ReadType.ZERO ? new FGuid() : Ar.Read<FGuid>(),
                "NavAgentSelector" => type == ReadType.ZERO ? new FNavAgentSelector() : Ar.Read<FNavAgentSelector>(),
                "SmartName" => type == ReadType.ZERO ? new FSmartName() : Ar.Read<FSmartName>(),
                "RichCurveKey" => type == ReadType.ZERO ? new FRichCurveKey() : Ar.Read<FRichCurveKey>(),
                "SimpleCurveKey" => type == ReadType.ZERO ? new FSimpleCurveKey() : Ar.Read<FSimpleCurveKey>(),
                "ColorMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<FColor>() : new FMaterialInput<FColor>(Ar),
                "ScalarMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<float>() : new FMaterialInput<float>(Ar),
                "ShadingModelMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<uint>() : new FMaterialInput<uint>(Ar),
                "VectorMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<FVector>() : new FMaterialInput<FVector>(Ar),
                "Vector2MaterialInput" => type == ReadType.ZERO ? new FMaterialInput<FVector2D>() : new FMaterialInput<FVector2D>(Ar),
                "ExpressionInput" => type == ReadType.ZERO ? new FExpressionInput() : new FExpressionInput(Ar),
                "MaterialAttributesInput" => type == ReadType.ZERO ? new FExpressionInput() : new FExpressionInput(Ar),
                "SkeletalMeshSamplingLODBuiltData" => type == ReadType.ZERO ? new FSkeletalMeshSamplingLODBuiltData() : new FSkeletalMeshSamplingLODBuiltData(Ar),
                "PerPlatformBool" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformBool() : new TPerPlatformProperty.FPerPlatformBool(Ar),
                "PerPlatformFloat" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformFloat() : new TPerPlatformProperty.FPerPlatformFloat(Ar),
                "PerPlatformInt" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformInt() : new TPerPlatformProperty.FPerPlatformInt(Ar),
                "GameplayTagContainer" => type == ReadType.ZERO ? new FGameplayTagContainer() : new FGameplayTagContainer(Ar),
                "LevelSequenceObjectReferenceMap" => type == ReadType.ZERO ? new FLevelSequenceObjectReferenceMap() : new FLevelSequenceObjectReferenceMap(Ar),
                "MovieSceneEvaluationKey" => type == ReadType.ZERO ? new FMovieSceneEvaluationKey() : Ar.Read<FMovieSceneEvaluationKey>(),
                "MovieSceneFloatValue" => type == ReadType.ZERO ? new FMovieSceneFloatValue() : Ar.Read<FMovieSceneFloatValue>(),
                "MovieSceneFrameRange" => type == ReadType.ZERO ? new FMovieSceneFrameRange() : Ar.Read<FMovieSceneFrameRange>(),
                "MovieSceneSegment" => type == ReadType.ZERO ? new FMovieSceneSegment() : new FMovieSceneSegment(Ar),
                "MovieSceneSegmentIdentifier" => type == ReadType.ZERO ? new FMovieSceneSegmentIdentifier() : Ar.Read<FMovieSceneSegmentIdentifier>(),
                "MovieSceneSequenceID" => type == ReadType.ZERO ? new FMovieSceneSequenceID() : Ar.Read<FMovieSceneSequenceID>(),
                "MovieSceneTrackIdentifier" => type == ReadType.ZERO ? new FMovieSceneTrackIdentifier() : Ar.Read<FMovieSceneTrackIdentifier>(),
                "SectionEvaluationDataTree" => type == ReadType.ZERO ? new FSectionEvaluationDataTree() : new FSectionEvaluationDataTree(Ar), // Deprecated in UE4.26? can't find it anymore. Replaced by FMovieSceneEvaluationTrack
                "MovieSceneFloatChannel" => type == ReadType.ZERO ? new MovieSceneFloatChannel() : new MovieSceneFloatChannel(Ar),
                "SoftObjectPath" => type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar),
                "SoftClassPath" => type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar),
                _ => type == ReadType.ZERO ? new FStructFallback() : new FStructFallback(Ar, structName)
            };
        }

        public override string ToString() => $"{StructType} ({StructType.GetType().Name})";
    }
}
