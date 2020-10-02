using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.LevelSequence;
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
                "Box" => new FBox(Ar),
                "Box2D" => new FBox2D(Ar),
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
                "GameplayTagContainer" => new FGameplayTagContainer(Ar),
                "LevelSequenceObjectReferenceMap" => new FLevelSequenceObjectReferenceMap(Ar),
                "SoftObjectPath" => new FSoftObjectPath(Ar),
                "SoftClassPath" => new FSoftObjectPath(Ar),

                "SimpleCurveKey" => throw new System.NotImplementedException(),
                "RichCurveKey" => throw new System.NotImplementedException(),
                "SmartName" => throw new System.NotImplementedException(),
                "PerPlatformFloat" => throw new System.NotImplementedException(),
                "PerPlatformInt" => throw new System.NotImplementedException(),
                "PerPlatformBool" => throw new System.NotImplementedException(),
                "SkeletalMeshSamplingLODBuiltData" => throw new System.NotImplementedException(),
                "NavAgentSelector" => throw new System.NotImplementedException(),
                "MovieSceneTrackIdentifier" => throw new System.NotImplementedException(),
                "MovieSceneSegmentIdentifier" => throw new System.NotImplementedException(),
                "MovieSceneSequenceID" => throw new System.NotImplementedException(),
                "MovieSceneSegment" => throw new System.NotImplementedException(),
                "SectionEvaluationDataTree" => throw new System.NotImplementedException(),
                "MovieSceneFrameRange" => throw new System.NotImplementedException(),
                "MovieSceneFloatValue" => throw new System.NotImplementedException(),
                "MovieSceneFloatChannel" => throw new System.NotImplementedException(),
                "MovieSceneEvaluationTemplate" => throw new System.NotImplementedException(),
                "MovieSceneEvaluationKey" => throw new System.NotImplementedException(),
                "VectorMaterialInput" => throw new System.NotImplementedException(),
                "ColorMaterialInput" => throw new System.NotImplementedException(),
                "ExpressionInput" => throw new System.NotImplementedException(),
                "ScalarMaterialInput" => throw new System.NotImplementedException(),
                "MaterialAttributesInput" => throw new System.NotImplementedException(),
                "Vector2MaterialInput" => throw new System.NotImplementedException(),
                _  => throw new System.NotImplementedException()
            };
        }
    }
}
