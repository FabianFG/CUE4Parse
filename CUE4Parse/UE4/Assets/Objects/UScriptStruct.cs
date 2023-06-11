using System;
using CUE4Parse.GameTypes.FN.Objects;
using CUE4Parse.GameTypes.SWJS.Objects;
using CUE4Parse.GameTypes.TSW.Objects;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Ai;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.Engine.GameFramework;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.LevelSequence;
using CUE4Parse.UE4.Objects.MovieScene;
using CUE4Parse.UE4.Objects.MovieScene.Evaluation;
using CUE4Parse.UE4.Objects.Niagara;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Objects.WorldCondition;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(UScriptStructConverter))]
    public class UScriptStruct
    {
        public readonly IUStruct StructType;

        public UScriptStruct(FAssetArchive Ar, string? structName, UStruct? struc, ReadType? type)
        {
            StructType = structName switch
            {
                "Box" => type == ReadType.ZERO ? new FBox() : new FBox(Ar),
                "Box2D" => type == ReadType.ZERO ? new FBox2D() : new FBox2D(Ar),
                "Color" => type == ReadType.ZERO ? new FColor() : Ar.Read<FColor>(),
                "ColorMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<FColor>() : new FMaterialInput<FColor>(Ar),
                "DateTime" => type == ReadType.ZERO ? new FDateTime() : Ar.Read<FDateTime>(),
                "ExpressionInput" => type == ReadType.ZERO ? new FExpressionInput() : new FExpressionInput(Ar),
                "FrameNumber" => type == ReadType.ZERO ? new FFrameNumber() : Ar.Read<FFrameNumber>(),
                "Guid" => type == ReadType.ZERO ? new FGuid() : Ar.Read<FGuid>(),
                "NavAgentSelector" => type == ReadType.ZERO ? new FNavAgentSelector() : Ar.Read<FNavAgentSelector>(),
                "SmartName" => type == ReadType.ZERO ? new FSmartName() : new FSmartName(Ar),
                "RichCurveKey" => type == ReadType.ZERO ? new FRichCurveKey() : Ar.Read<FRichCurveKey>(),
                "SimpleCurveKey" => type == ReadType.ZERO ? new FSimpleCurveKey() : Ar.Read<FSimpleCurveKey>(),
                "ScalarMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<float>() : new FMaterialInput<float>(Ar),
                "ShadingModelMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<uint>() : new FMaterialInput<uint>(Ar),
                "VectorMaterialInput" => type == ReadType.ZERO ? new FMaterialInputVector() : new FMaterialInputVector(Ar),
                "Vector2MaterialInput" => type == ReadType.ZERO ? new FMaterialInputVector2D() : new FMaterialInputVector2D(Ar),
                "MaterialAttributesInput" => type == ReadType.ZERO ? new FExpressionInput() : new FExpressionInput(Ar),
                "SkeletalMeshSamplingLODBuiltData" => type == ReadType.ZERO ? new FSkeletalMeshSamplingLODBuiltData() : new FSkeletalMeshSamplingLODBuiltData(Ar),
                "SkeletalMeshSamplingRegionBuiltData" => type == ReadType.ZERO ? new FSkeletalMeshSamplingRegionBuiltData() : new FSkeletalMeshSamplingRegionBuiltData(Ar),
                "PerPlatformBool" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformBool() : new TPerPlatformProperty.FPerPlatformBool(Ar),
                "PerPlatformFloat" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformFloat() : new TPerPlatformProperty.FPerPlatformFloat(Ar),
                "PerPlatformInt" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformInt() : new TPerPlatformProperty.FPerPlatformInt(Ar),
                "PerPlatformFrameRate" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformFrameRate() : new TPerPlatformProperty.FPerPlatformFrameRate(Ar),
                "PerQualityLevelInt" => type == ReadType.ZERO ? new FPerQualityLevelInt() : new FPerQualityLevelInt(Ar),
                "GameplayTagContainer" => type == ReadType.ZERO ? new FGameplayTagContainer() : new FGameplayTagContainer(Ar),
                "IntPoint" => type == ReadType.ZERO ? new FIntPoint() : Ar.Read<FIntPoint>(),
                "IntVector" => type == ReadType.ZERO ? new FIntVector() : Ar.Read<FIntVector>(),
                "LevelSequenceObjectReferenceMap" => type == ReadType.ZERO ? new FLevelSequenceObjectReferenceMap() : new FLevelSequenceObjectReferenceMap(Ar),
                "LinearColor" => type == ReadType.ZERO ? new FLinearColor() : Ar.Read<FLinearColor>(),
                "NiagaraVariable" => new FNiagaraVariable(Ar),
                "NiagaraVariableBase" => new FNiagaraVariableBase(Ar),
                "NiagaraVariableWithOffset" => new FNiagaraVariableWithOffset(Ar),
                "NiagaraDataInterfaceGPUParamInfo" => new FNiagaraDataInterfaceGPUParamInfo(Ar),
                "MaterialOverrideNanite" => type == ReadType.ZERO ? new FMaterialOverrideNanite() : new FMaterialOverrideNanite(Ar),
                "MovieSceneEvalTemplatePtr" => new FMovieSceneEvalTemplatePtr(Ar),
                "MovieSceneEvaluationFieldEntityTree" => new FMovieSceneEvaluationFieldEntityTree(Ar),
                "MovieSceneEvaluationKey" => type == ReadType.ZERO ? new FMovieSceneEvaluationKey() : Ar.Read<FMovieSceneEvaluationKey>(),
                "MovieSceneFloatChannel" => type == ReadType.ZERO ? new FMovieSceneChannel<float>() : new FMovieSceneChannel<float>(Ar),
                "MovieSceneDoubleChannel" => type == ReadType.ZERO ? new FMovieSceneChannel<double>() : new FMovieSceneChannel<double>(Ar),
                "MovieSceneFloatValue" => type == ReadType.ZERO ? new FMovieSceneValue<float>() : Ar.Read<FMovieSceneValue<float>>(),
                "MovieSceneDoubleValue" => type == ReadType.ZERO ? new FMovieSceneValue<double>() : Ar.Read<FMovieSceneValue<double>>(),
                "MovieSceneFrameRange" => type == ReadType.ZERO ? new FMovieSceneFrameRange() : Ar.Read<FMovieSceneFrameRange>(),
                "MovieSceneSegment" => type == ReadType.ZERO ? new FMovieSceneSegment() : new FMovieSceneSegment(Ar),
                "MovieSceneSegmentIdentifier" => type == ReadType.ZERO ? new FMovieSceneSegmentIdentifier() : Ar.Read<FMovieSceneSegmentIdentifier>(),
                "MovieSceneSequenceID" => type == ReadType.ZERO ? new FMovieSceneSequenceID() : Ar.Read<FMovieSceneSequenceID>(),
                "MovieSceneTrackIdentifier" => type == ReadType.ZERO ? new FMovieSceneTrackIdentifier() : Ar.Read<FMovieSceneTrackIdentifier>(),
                "MovieSceneTrackImplementationPtr" => new FMovieSceneTrackImplementationPtr(Ar),
                "FontData" => new FFontData(Ar),
                "FontCharacter" => new FFontCharacter(Ar),
                "Plane" => type == ReadType.ZERO ? new FPlane() : new FPlane(Ar),
                "Plane4f" => type == ReadType.ZERO ? new FPlane() : new FPlane(Ar.Read<TIntVector3<float>>(), Ar.Read<float>()),
                "Plane4d" => type == ReadType.ZERO ? new FPlane() : new FPlane(Ar.Read<TIntVector3<double>>(), Ar.Read<double>()),
                "Quat" => type == ReadType.ZERO ? new FQuat() : new FQuat(Ar),
                "Quat4f" => type == ReadType.ZERO ? new FQuat() : new FQuat(Ar.Read<TIntVector4<float>>()),
                "Quat4d" => type == ReadType.ZERO ? new FQuat() : new FQuat(Ar.Read<TIntVector4<double>>()),
                "Rotator" => type == ReadType.ZERO ? new FRotator() : new FRotator(Ar),
                "Rotator3f" => type == ReadType.ZERO ? new FRotator() : new FRotator(Ar.Read<float>(), Ar.Read<float>(), Ar.Read<float>()),
                "Rotator3d" => type == ReadType.ZERO ? new FRotator() : new FRotator(Ar.Read<double>(), Ar.Read<double>(), Ar.Read<double>()),
                "Sphere" => type == ReadType.ZERO ? new FSphere() : new FSphere(Ar),
                "Sphere3f" => type == ReadType.ZERO ? new FSphere() : new FSphere(Ar.Read<TIntVector3<float>>(), Ar.Read<float>()),
                "Sphere3d" => type == ReadType.ZERO ? new FSphere() : new FSphere(Ar.Read<TIntVector3<double>>(), Ar.Read<double>()),
                "SectionEvaluationDataTree" => type == ReadType.ZERO ? new FSectionEvaluationDataTree() : new FSectionEvaluationDataTree(Ar), // Deprecated in UE4.26? can't find it anymore. Replaced by FMovieSceneEvaluationTrack
                "StringClassReference" => type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar),
                "SoftClassPath" => type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar),
                "StringAssetReference" => type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar),
                "SoftObjectPath" => type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar),
                "Timespan" => type == ReadType.ZERO ? new FDateTime() : Ar.Read<FDateTime>(),
                "UniqueNetIdRepl" => new FUniqueNetIdRepl(Ar),
                "Vector" => type == ReadType.ZERO ? new FVector() : new FVector(Ar),
                "Vector2D" => type == ReadType.ZERO ? new FVector2D() : new FVector2D(Ar),
                "Vector2f" => type == ReadType.ZERO ? new TIntVector2<float>() : Ar.Read<TIntVector2<float>>(),
                "DeprecateSlateVector2D" => type == ReadType.ZERO ? new FVector2D() : Ar.Read<FVector2D>(),
                "Vector3f" => type == ReadType.ZERO ? new TIntVector3<float>() : Ar.Read<TIntVector3<float>>(),
                "Vector3d" => type == ReadType.ZERO ? new TIntVector3<double>() : Ar.Read<TIntVector3<double>>(),
                "Vector4" => type == ReadType.ZERO ? new FVector4() : new FVector4(Ar),
                "Vector4f" => type == ReadType.ZERO ? new TIntVector4<float>() : Ar.Read<TIntVector4<float>>(),
                "Vector_NetQuantize" => type == ReadType.ZERO ? new FVector() : new FVector(Ar),
                "Vector_NetQuantize10" => type == ReadType.ZERO ? new FVector() : new FVector(Ar),
                "Vector_NetQuantize100" => type == ReadType.ZERO ? new FVector() : new FVector(Ar),
                "Vector_NetQuantizeNormal" => type == ReadType.ZERO ? new FVector() : new FVector(Ar),
                "ClothLODDataCommon" => type == ReadType.ZERO ? new FClothLODDataCommon() : new FClothLODDataCommon(Ar),
                "ClothTetherData" => type == ReadType.ZERO ? new FClothTetherData() : new FClothTetherData(Ar),
                "Matrix" => type == ReadType.ZERO ? new FMatrix() : new FMatrix(Ar),
                "InstancedStruct" => new FInstancedStruct(Ar),
                "WorldConditionQueryDefinition" => new FWorldConditionQueryDefinition(Ar),

                // FortniteGame
                "ConnectivityCube" => new FConnectivityCube(Ar),
                //"FortActorRecord" => new FFortActorRecord(Ar),

                // Train Sim World
                "DistanceQuantity" => Ar.Read<FDistanceQuantity>(),
                "SpeedQuantity" => Ar.Read<FSpeedQuantity>(),
                "MassQuantity" => Ar.Read<FMassQuantity>(),

                // GTA: The Trilogy
                "ScalarParameterValue" when Ar.Game == EGame.GAME_GTATheTrilogyDefinitiveEdition => new FScalarParameterValue(Ar),
                "VectorParameterValue" when Ar.Game == EGame.GAME_GTATheTrilogyDefinitiveEdition => new FVectorParameterValue(Ar),
                "TextureParameterValue" when Ar.Game == EGame.GAME_GTATheTrilogyDefinitiveEdition => new FTextureParameterValue(Ar),
                "MaterialTextureInfo" when Ar.Game == EGame.GAME_GTATheTrilogyDefinitiveEdition => new FMaterialTextureInfo(Ar),

                // STAR WARS Jedi: Survivor
                "SwBitfield_TargetRotatorMask" => new FRsBitfield(Ar, structName),
                "RsBitfield_NavPermissionDetailFlags" => new FRsBitfield(Ar, structName),
                "RsBitfield_NavPermissionFlags" => new FRsBitfield(Ar, structName),
                "RsBitfield_NavState" => new FRsBitfield(Ar, structName),
                "RsBitfield_HeroLoadoutFlags" => new FRsBitfield(Ar, structName),
                "RsBitfield_HeroBufferFlags" => new FRsBitfield(Ar, structName),
                "RsBitfield_HeroInputFlags" => new FRsBitfield(Ar, structName),
                "RsBitfield_HeroUpgradeFlags" => new FRsBitfield(Ar, structName),
                "RsBitfield_RsIkBoneTypes" => new FRsBitfield(Ar, structName),
                "RsBitfield_UINavigationInput" => new FRsBitfield(Ar, structName),
                "RsBitfield_WorldMapLevelType" => new FRsBitfield(Ar, structName),
                "RsBitfield_WorldMapLODLevel" => new FRsBitfield(Ar, structName),
                "RsBitfield_WorldMapWidgetFilterType" => new FRsBitfield(Ar, structName),

                _ => type == ReadType.ZERO ? new FStructFallback() : struc != null ? new FStructFallback(Ar, struc) : new FStructFallback(Ar, structName)
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
