using System;
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
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(UScriptStructConverter))]
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
                "ColorMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<FColor>() : new FMaterialInput<FColor>(Ar),
                "DateTime" => type == ReadType.ZERO ? new FDateTime() : Ar.Read<FDateTime>(),
                "ExpressionInput" => type == ReadType.ZERO ? new FExpressionInput() : new FExpressionInput(Ar),
                "FrameNumber" => type == ReadType.ZERO ? new FFrameNumber() : Ar.Read<FFrameNumber>(),
                "FrameRate" => type == ReadType.ZERO ? new FFrameRate() : Ar.Read<FFrameRate>(),
                "GameplayTagContainer" => type == ReadType.ZERO ? new FGameplayTagContainer() : new FGameplayTagContainer(Ar),
                "Guid" => type == ReadType.ZERO ? new FGuid() : Ar.Read<FGuid>(),
                "IntPoint" => type == ReadType.ZERO ? new FIntPoint() : Ar.Read<FIntPoint>(),
                "IntVector" => type == ReadType.ZERO ? new FIntVector() : Ar.Read<FIntVector>(),
                "LevelSequenceObjectReferenceMap" => type == ReadType.ZERO ? new FLevelSequenceObjectReferenceMap() : new FLevelSequenceObjectReferenceMap(Ar),
                "LinearColor" => type == ReadType.ZERO ? new FLinearColor() : Ar.Read<FLinearColor>(),
                "MaterialAttributesInput" => type == ReadType.ZERO ? new FExpressionInput() : new FExpressionInput(Ar),
                "MovieSceneEvaluationKey" => type == ReadType.ZERO ? new FMovieSceneEvaluationKey() : Ar.Read<FMovieSceneEvaluationKey>(),
                "MovieSceneFloatChannel" => type == ReadType.ZERO ? new MovieSceneFloatChannel() : new MovieSceneFloatChannel(Ar),
                "MovieSceneFloatValue" => type == ReadType.ZERO ? new FMovieSceneFloatValue() : Ar.Read<FMovieSceneFloatValue>(),
                "MovieSceneFrameRange" => type == ReadType.ZERO ? new FMovieSceneFrameRange() : Ar.Read<FMovieSceneFrameRange>(),
                "MovieSceneSegment" => type == ReadType.ZERO ? new FMovieSceneSegment() : new FMovieSceneSegment(Ar),
                "MovieSceneSegmentIdentifier" => type == ReadType.ZERO ? new FMovieSceneSegmentIdentifier() : Ar.Read<FMovieSceneSegmentIdentifier>(),
                "MovieSceneSequenceID" => type == ReadType.ZERO ? new FMovieSceneSequenceID() : Ar.Read<FMovieSceneSequenceID>(),
                "MovieSceneTrackIdentifier" => type == ReadType.ZERO ? new FMovieSceneTrackIdentifier() : Ar.Read<FMovieSceneTrackIdentifier>(),
                "NavAgentSelector" => type == ReadType.ZERO ? new FNavAgentSelector() : Ar.Read<FNavAgentSelector>(),
                "PerPlatformBool" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformBool() : new TPerPlatformProperty.FPerPlatformBool(Ar),
                "PerPlatformFloat" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformFloat() : new TPerPlatformProperty.FPerPlatformFloat(Ar),
                "PerPlatformInt" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformInt() : new TPerPlatformProperty.FPerPlatformInt(Ar),
                "Quat" => type == ReadType.ZERO ? new FQuat() : Ar.Read<FQuat>(),
                "RichCurveKey" => type == ReadType.ZERO ? new FRichCurveKey() : Ar.Read<FRichCurveKey>(),
                "Rotator" => type == ReadType.ZERO ? new FRotator() : Ar.Read<FRotator>(),
                "ScalarMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<float>() : new FMaterialInput<float>(Ar),
                "SectionEvaluationDataTree" => type == ReadType.ZERO ? new FSectionEvaluationDataTree() : new FSectionEvaluationDataTree(Ar), // Deprecated in UE4.26? can't find it anymore. Replaced by FMovieSceneEvaluationTrack
                "ShadingModelMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<uint>() : new FMaterialInput<uint>(Ar),
                "SimpleCurveKey" => type == ReadType.ZERO ? new FSimpleCurveKey() : Ar.Read<FSimpleCurveKey>(),
                "SkeletalMeshSamplingLODBuiltData" => type == ReadType.ZERO ? new FSkeletalMeshSamplingLODBuiltData() : new FSkeletalMeshSamplingLODBuiltData(Ar),
                "SmartName" => type == ReadType.ZERO ? new FSmartName() : Ar.Read<FSmartName>(),
                "SoftClassPath" => type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar),
                "SoftObjectPath" => type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar),
                "Timespan" => type == ReadType.ZERO ? new FDateTime() : Ar.Read<FDateTime>(),
                "Vector" => type == ReadType.ZERO ? new FVector() : Ar.Read<FVector>(),
                "Vector2D" => type == ReadType.ZERO ? new FVector2D() : Ar.Read<FVector2D>(),
                "Vector2MaterialInput" => type == ReadType.ZERO ? new FMaterialInput<FVector2D>() : new FMaterialInput<FVector2D>(Ar),
                "Vector4" => type == ReadType.ZERO ? new FVector4() : Ar.Read<FVector4>(),
                "VectorMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<FVector>() : new FMaterialInput<FVector>(Ar),
                _ => type == ReadType.ZERO ? new FStructFallback() : new FStructFallback(Ar, structName)
            };
        }

        public override string ToString() => $"{StructType} ({StructType.GetType().Name})";
    }
    
    public class UScriptStructConverter : JsonConverter<UScriptStruct>
    {
        public override void WriteJson(JsonWriter writer, UScriptStruct value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.StructType);
        }

        public override UScriptStruct ReadJson(JsonReader reader, Type objectType, UScriptStruct existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
