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

        public UScriptStruct(FAssetArchive Ar, string? structName)
        {
            StructType = structName switch
            {
                "Box" => Ar.Read<FBox>(),
                "Box2D" => Ar.Read<FBox2D>(),
                "Color" => Ar.Read<FColor>(),
                "IntPoint" => Ar.Read<FIntPoint>(),
                "IntVector" => Ar.Read<FIntVector>(),
                "LinearColor" => Ar.Read<FLinearColor>(),
                "Quat" => Ar.Read<FQuat>(),
                "Rotator" => Ar.Read<FRotator>(),
                "Vector" => Ar.Read<FVector>(),
                "Vector2D" => Ar.Read<FVector2D>(),
                "Vector4" => Ar.Read<FVector4>(),
                "DateTime" => Ar.Read<FDateTime>(),
                "Timespan" => Ar.Read<FDateTime>(),
                "FrameNumber" => Ar.Read<FFrameNumber>(),
                "FrameRate" => Ar.Read<FFrameRate>(),
                "Guid" => Ar.Read<FGuid>(),
                "NavAgentSelector" => Ar.Read<FNavAgentSelector>(),
                "SmartName" => new FSmartName(Ar),
                "RichCurveKey" => Ar.Read<FRichCurveKey>(),
                "SimpleCurveKey" => Ar.Read<FSimpleCurveKey>(),
                "ColorMaterialInput" => new FMaterialInput<FColor>(Ar),
                "ScalarMaterialInput" => new FMaterialInput<float>(Ar),
                "ShadingModelMaterialInput" => new FMaterialInput<uint>(Ar),
                "VectorMaterialInput" => new FMaterialInput<FVector>(Ar),
                "Vector2MaterialInput" => new FMaterialInput<FVector2D>(Ar),
                "ExpressionInput" => new FExpressionInput(Ar),
                "MaterialAttributesInput" => new FExpressionInput(Ar),
                "SkeletalMeshSamplingLODBuiltData" => new FSkeletalMeshSamplingLODBuiltData(Ar),
                "PerPlatformBool" => new TPerPlatformProperty.FPerPlatformBool(Ar),
                "PerPlatformFloat" => new TPerPlatformProperty.FPerPlatformFloat(Ar),
                "PerPlatformInt" => new TPerPlatformProperty.FPerPlatformInt(Ar),
                "GameplayTagContainer" => new FGameplayTagContainer(Ar),
                "LevelSequenceObjectReferenceMap" => new FLevelSequenceObjectReferenceMap(Ar),
                "MovieSceneEvaluationKey" => Ar.Read<FMovieSceneEvaluationKey>(),
                "MovieSceneFloatValue" => Ar.Read<FMovieSceneFloatValue>(),
                "MovieSceneFrameRange" => Ar.Read<FMovieSceneFrameRange>(),
                "MovieSceneSegment" => new FMovieSceneSegment(Ar),
                "MovieSceneSegmentIdentifier" => Ar.Read<FMovieSceneSegmentIdentifier>(),
                "MovieSceneSequenceID" => Ar.Read<FMovieSceneSequenceID>(),
                "MovieSceneTrackIdentifier" => Ar.Read<FMovieSceneTrackIdentifier>(),
                "SectionEvaluationDataTree" => new FSectionEvaluationDataTree(Ar), // Deprecated in UE4.26? can't find it anymore. Replaced by FMovieSceneEvaluationTrack
                "MovieSceneFloatChannel" => new MovieSceneFloatChannel(Ar),
                "SoftObjectPath" => new FSoftObjectPath(Ar),
                "SoftClassPath" => new FSoftObjectPath(Ar),
                _ => new FStructFallback(Ar, structName),
            };
        }

        public override string ToString() => $"{StructType} ({StructType.GetType().Name})";
    }
}
