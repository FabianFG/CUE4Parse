using CUE4Parse.GameTypes.Brickadia.Objects;
using CUE4Parse.GameTypes.DuneAwakening.Assets.Objects;
using CUE4Parse.GameTypes.FN.Objects;
using CUE4Parse.GameTypes.Gothic1R.Assets.Objects;
using CUE4Parse.GameTypes.L2KD.Objects;
using CUE4Parse.GameTypes.MA.Objects;
using CUE4Parse.GameTypes.MindsEye.Objects;
using CUE4Parse.GameTypes.NetEase.MAR.Objects;
using CUE4Parse.GameTypes.OtherGames.Objects;
using CUE4Parse.GameTypes.SG2.Objects;
using CUE4Parse.GameTypes.SOD2.Assets.Objects;
using CUE4Parse.GameTypes.SWJS.Objects;
using CUE4Parse.GameTypes.TL.Objects;
using CUE4Parse.GameTypes.TSW.Objects;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.ChaosCaching;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Ai;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.Engine.ComputeFramework;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.Engine.GameFramework;
using CUE4Parse.UE4.Objects.Engine.Material;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.LevelSequence;
using CUE4Parse.UE4.Objects.MovieScene;
using CUE4Parse.UE4.Objects.MovieScene.Evaluation;
using CUE4Parse.UE4.Objects.Niagara;
using CUE4Parse.UE4.Objects.PCG;
using CUE4Parse.UE4.Objects.StateTree;
using CUE4Parse.UE4.Objects.StructUtils;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Objects.WorldCondition;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(FScriptStructConverter))]
public class FScriptStruct
{
    public readonly IUStruct StructType;

    public FScriptStruct(FAssetArchive Ar, string? structName, UStruct? struc, ReadType? type)
    {
        StructType = structName switch
        {
            "Box" => type == ReadType.ZERO ? new FBox() : new FBox(Ar),
            "Box2D" => type == ReadType.ZERO ? new FBox2D() : new FBox2D(Ar),
            "Box2f" => type == ReadType.ZERO ? new TBox2<float>() : new TBox2<float>(Ar),
            "Color" => type == ReadType.ZERO ? new FColor() : Ar.Read<FColor>(),
            "ColorMaterialInput" when FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.MaterialInputUsesLinearColor
                => type == ReadType.ZERO ? new FMaterialInput<FColor>() : new FMaterialInput<FColor>(Ar),
            "ColorMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<FLinearColor>() : new FMaterialInput<FLinearColor>(Ar),
            "CompressedRichCurve" => type == ReadType.ZERO ? new FStructFallback() : new FCompressedRichCurve(Ar),
            "DateTime" => type == ReadType.ZERO ? new FDateTime() : Ar.Read<FDateTime>(),
            "ExpressionInput" => type == ReadType.ZERO ? new FExpressionInput() : new FExpressionInput(Ar),
            "FrameNumber" => type == ReadType.ZERO ? new FFrameNumber() : Ar.Read<FFrameNumber>(),
            "Guid" => type == ReadType.ZERO ? new FGuid() : Ar.Read<FGuid>(),
            "NavAgentSelector" => type == ReadType.ZERO ? new FNavAgentSelector() : Ar.Read<FNavAgentSelector>(),
            "SmartName" => type == ReadType.ZERO ? new FSmartName() : new FSmartName(Ar),
            "NameCurveKey" => type == ReadType.ZERO ? new FNameCurveKey() : new FNameCurveKey(Ar),
            "RichCurveKey" => type == ReadType.ZERO ? new FRichCurveKey() : Ar.Read<FRichCurveKey>(),
            "SimpleCurveKey" => type == ReadType.ZERO ? new FSimpleCurveKey() : Ar.Read<FSimpleCurveKey>(),
            "ScalarMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<float>() : new FMaterialInput<float>(Ar),
            //"ShadingModelMaterialInput" => type == ReadType.ZERO ? new FMaterialInput<uint>() : new FMaterialInput<uint>(Ar),
            "VectorMaterialInput" => type == ReadType.ZERO ? new FMaterialInputVector() : new FMaterialInput<FVector>(Ar),
            "Vector2MaterialInput" => type == ReadType.ZERO ? new FMaterialInputVector2D() : new FMaterialInput<FVector2D>(Ar),
            "MaterialAttributesInput" => type == ReadType.ZERO ? new FExpressionInput() : new FExpressionInput(Ar),
            "SkeletalMeshSamplingLODBuiltData" => type == ReadType.ZERO ? new FSkeletalMeshSamplingLODBuiltData() : new FSkeletalMeshSamplingLODBuiltData(Ar),
            "SkeletalMeshSamplingRegionBuiltData" => type == ReadType.ZERO ? new FSkeletalMeshSamplingRegionBuiltData() : new FSkeletalMeshSamplingRegionBuiltData(Ar),
            "PerPlatformBool" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformBool() : new TPerPlatformProperty.FPerPlatformBool(Ar),
            "PerPlatformFloat" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformFloat() : new TPerPlatformProperty.FPerPlatformFloat(Ar),
            "PerPlatformInt" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformInt() : new TPerPlatformProperty.FPerPlatformInt(Ar),
            "PerPlatformFrameRate" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformFrameRate() : new TPerPlatformProperty.FPerPlatformFrameRate(Ar),
            "PerPlatformFString" => type == ReadType.ZERO ? new TPerPlatformProperty.FPerPlatformFString() : new TPerPlatformProperty.FPerPlatformFString(Ar),
            "PerQualityLevelInt" => type == ReadType.ZERO ? new FPerQualityLevelInt() : new FPerQualityLevelInt(Ar),
            "PerQualityLevelFloat" => type == ReadType.ZERO ? new FPerQualityLevelFloat() : new FPerQualityLevelFloat(Ar),
            "GameplayTagContainer" => type == ReadType.ZERO ? new FGameplayTagContainer() : new FGameplayTagContainer(Ar),
            "IntPoint" or "Int32Point" => type == ReadType.ZERO ? new FIntPoint() : Ar.Read<FIntPoint>(),
            "IntVector2" => type == ReadType.ZERO ? new TIntVector2<int>() : Ar.Read<TIntVector2<int>>(),
            "UintVector2" => type == ReadType.ZERO ? new TIntVector2<uint>() : Ar.Read<TIntVector2<uint>>(),
            "IntVector" => type == ReadType.ZERO ? new FIntVector() : Ar.Read<FIntVector>(),
            "UintVector" => type == ReadType.ZERO ? new TIntVector3<uint>() : Ar.Read<TIntVector3<uint>>(),
            "IntVector4" => type == ReadType.ZERO ? new TIntVector4<int>() : Ar.Read<TIntVector4<int>>(),
            "UintVector4" => type == ReadType.ZERO ? new TIntVector4<uint>() : Ar.Read<TIntVector4<uint>>(),
            "LevelSequenceObjectReferenceMap" => type == ReadType.ZERO ? new FLevelSequenceObjectReferenceMap() : new FLevelSequenceObjectReferenceMap(Ar),
            "LinearColor" => type == ReadType.ZERO ? new FLinearColor() : Ar.Read<FLinearColor>(),
            "NiagaraVariable" => new FNiagaraVariable(Ar),
            "NiagaraVariableBase" or "NiagaraDataChannelVariable" => new FNiagaraVariableBase(Ar),
            "NiagaraVariableWithOffset" => new FNiagaraVariableWithOffset(Ar),
            "NiagaraDataInterfaceGPUParamInfo" => new FNiagaraDataInterfaceGPUParamInfo(Ar),
            "MaterialOverrideNanite" => type == ReadType.ZERO ? new FMaterialOverrideNanite() : new FMaterialOverrideNanite(Ar),
            "MaterialLayersFunctionsTree" => type == ReadType.ZERO ? new FMaterialLayersFunctionsTree() : new FMaterialLayersFunctionsTree(Ar),
            "MovieSceneEvalTemplatePtr" => new FMovieSceneEvalTemplatePtr(Ar),
            "MovieSceneEvaluationFieldEntityTree" => new FMovieSceneEvaluationFieldEntityTree(Ar),
            "MovieSceneEvaluationKey" => type == ReadType.ZERO ? new FMovieSceneEvaluationKey() : Ar.Read<FMovieSceneEvaluationKey>(),
            "MovieSceneEventParameters" => type == ReadType.ZERO ? new FMovieSceneEventParameters() : new FMovieSceneEventParameters(Ar),
            "MovieSceneFloatChannel" => type == ReadType.ZERO ? new FMovieSceneChannel<float>() : new FMovieSceneChannel<float>(Ar),
            "MovieSceneDoubleChannel" => type == ReadType.ZERO ? new FMovieSceneChannel<double>() : new FMovieSceneChannel<double>(Ar),
            "MovieSceneFloatValue" => type == ReadType.ZERO ? new FMovieSceneValue<float>() : new FMovieSceneValue<float>(Ar, Ar.Read<float>(), true),
            "MovieSceneDoubleValue" => type == ReadType.ZERO ? new FMovieSceneValue<double>() : new FMovieSceneValue<double>(Ar, Ar.Read<double>(), true),
            "MovieSceneFrameRange" => type == ReadType.ZERO ? new FMovieSceneFrameRange() : Ar.Read<FMovieSceneFrameRange>(),
            "MovieSceneSegment" => type == ReadType.ZERO ? new FMovieSceneSegment() : new FMovieSceneSegment(Ar),
            "MovieSceneSegmentIdentifier" => type == ReadType.ZERO ? new FMovieSceneSegmentIdentifier() : Ar.Read<FMovieSceneSegmentIdentifier>(),
            "MovieSceneSequenceID" => type == ReadType.ZERO ? new FMovieSceneSequenceID() : Ar.Read<FMovieSceneSequenceID>(),
            "MovieSceneSequenceInstanceDataPtr" => type == ReadType.ZERO ? new FMovieSceneSequenceInstanceDataPtr() : new FMovieSceneSequenceInstanceDataPtr(Ar),
            "MovieSceneSubSequenceTree" => type == ReadType.ZERO ? new FMovieSceneSubSequenceTree() : new FMovieSceneSubSequenceTree(Ar),
            "MovieSceneSubSectionFieldData" => type == ReadType.ZERO ? new FMovieSceneSubSectionFieldData() : new FMovieSceneSubSectionFieldData(Ar),
            "MovieSceneTimeWarpVariant" => type == ReadType.ZERO ? new FStructFallback() : new FMovieSceneTimeWarpVariant(Ar),
            "MovieSceneTrackFieldData" => type == ReadType.ZERO ? new FMovieSceneTrackFieldData() : new FMovieSceneTrackFieldData(Ar),
            "MovieSceneTrackIdentifier" => type == ReadType.ZERO ? new FMovieSceneTrackIdentifier() : new FMovieSceneTrackIdentifier(Ar),
            "MovieSceneTrackIdentifiers" => type == ReadType.ZERO ? new FMovieSceneTrackIdentifiers() : new FMovieSceneTrackIdentifiers(Ar),
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
            "RawAnimSequenceTrack" => new FRawAnimSequenceTrack(Ar),
            "Sphere" => type == ReadType.ZERO ? new FSphere() : new FSphere(Ar),
            "Sphere3f" => type == ReadType.ZERO ? new FSphere() : new FSphere(Ar.Read<TIntVector3<float>>(), Ar.Read<float>()),
            "Sphere3d" => type == ReadType.ZERO ? new FSphere() : new FSphere(Ar.Read<TIntVector3<double>>(), Ar.Read<double>()),
            "SectionEvaluationDataTree" => type == ReadType.ZERO ? new FSectionEvaluationDataTree() : new FSectionEvaluationDataTree(Ar), // Deprecated in UE4.26? can't find it anymore. Replaced by FMovieSceneEvaluationTrack
            "StringClassReference" => type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar),
            "SoftClassPath" => type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar),
            "StringAssetReference" => type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar),
            "SoftObjectPath" => type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar),
            "Timespan" => type == ReadType.ZERO ? new FDateTime() : Ar.Read<FDateTime>(),
            "Transform3f" => type == ReadType.ZERO ? new FTransform() : Ar.Read<FTransform>(),
            "TwoVectors" => type == ReadType.ZERO ? new FTwoVectors() : new FTwoVectors(Ar),
            "UniqueNetIdRepl" => new FUniqueNetIdRepl(Ar),
            "Vector" => type == ReadType.ZERO ? new FVector() : new FVector(Ar),
            "Vector2D" => type == ReadType.ZERO ? new FVector2D() : new FVector2D(Ar),
            "Vector2f" => type == ReadType.ZERO ? new TIntVector2<float>() : Ar.Read<TIntVector2<float>>(),
            "DeprecateSlateVector2D" => type == ReadType.ZERO ? new FVector2D() : Ar.Read<FVector2D>(),
            "Vector3f" => type == ReadType.ZERO ? new TIntVector3<float>() : Ar.Read<TIntVector3<float>>(),
            "Vector3d" => type == ReadType.ZERO ? new TIntVector3<double>() : Ar.Read<TIntVector3<double>>(),
            "Vector4" => type == ReadType.ZERO ? new FVector4() : new FVector4(Ar),
            "Vector4f" => type == ReadType.ZERO ? new TIntVector4<float>() : Ar.Read<TIntVector4<float>>(),
            "Vector4d" => type == ReadType.ZERO ? new TIntVector4<double>() : Ar.Read<TIntVector4<double>>(),
            "Vector_NetQuantize" => type == ReadType.ZERO ? new FVector() : Ar.Versions["Vector_NetQuantize_AsStruct"] ? new FStructFallback(Ar, "Vector_NetQuantize") : new FVector(Ar),
            "Vector_NetQuantize10" => type == ReadType.ZERO ? new FVector() : Ar.Versions["Vector_NetQuantize_AsStruct"] ? new FStructFallback(Ar, "Vector_NetQuantize") : new FVector(Ar),
            "Vector_NetQuantize100" => type == ReadType.ZERO ? new FVector() : Ar.Versions["Vector_NetQuantize_AsStruct"] ? new FStructFallback(Ar, "Vector_NetQuantize") : new FVector(Ar),
            "Vector_NetQuantizeNormal" => type == ReadType.ZERO ? new FVector() : Ar.Versions["Vector_NetQuantize_AsStruct"] ? new FStructFallback(Ar, "Vector_NetQuantize") : new FVector(Ar),
            "ClothLODDataCommon" => type == ReadType.ZERO ? new FClothLODDataCommon() : new FClothLODDataCommon(Ar),
            "ClothTetherData" => type == ReadType.ZERO ? new FClothTetherData() : new FClothTetherData(Ar),
            "Matrix" => type == ReadType.ZERO ? new FMatrix() : new FMatrix(Ar),
            "Matrix44f" => type == ReadType.ZERO ? new FMatrix() : new FMatrix(Ar, false),
            "InstancedStruct" => new FInstancedStruct(Ar),
            "InstancedStructContainer" => new FInstancedStructContainer(Ar),
            "InstancedPropertyBag" => new FInstancedPropertyBag(Ar),
            "WorldConditionQueryDefinition" => new FWorldConditionQueryDefinition(Ar),
            "UniversalObjectLocatorFragment" => type == ReadType.ZERO ? new FUniversalObjectLocatorFragment() : new FUniversalObjectLocatorFragment(Ar),
            "KeyHandleMap" => new FStructFallback(),
            "ShaderValueTypeHandle" => new FShaderValueTypeHandle(Ar),
            "AnimationAttributeIdentifier" => new FAnimationAttributeIdentifier(Ar),
            "AttributeCurve" => new FAttributeCurve(Ar),
            "PCGPoint" => FFortniteReleaseBranchCustomObjectVersion.Get(Ar) < FFortniteReleaseBranchCustomObjectVersion.Type.PCGPointStructuredSerializer ? new FStructFallback(Ar, "PCGPoint") : new FPCGPoint(Ar),
            "CacheEventTrack" => type == ReadType.ZERO ? new FStructFallback() : new FCacheEventTrack(Ar),
            "StateTreeInstanceData" => type == ReadType.ZERO ? new FStructFallback() : new FStateTreeInstanceData(Ar),
            
            // FortniteGame
            "ConnectivityCube" => new FConnectivityCube(Ar),
            "FortActorRecord" => new FFortActorRecord(Ar),

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

            // Lego 2K Drive
            "LegoGraphPartInstance" => type == ReadType.ZERO ? new FLegoGraphPartInstance() : new FLegoGraphPartInstance(Ar),

            // Splitgate2
            "Core1047ReleaseFlag" => new FCore1047ReleaseFlag(Ar),

            // ThroneAndLiberty
            "TLJsonGuid" => type == ReadType.ZERO ? new FGuid() : Ar.Read<FGuid>(),
            "TLJsonVector" => type == ReadType.ZERO ? new FVector() : new FVector(Ar),
            "TLJsonVector2D" => type == ReadType.ZERO ? new FVector2D() : new FVector2D(Ar),
            "SceneFaceDefSeamline" => new FSceneFaceDefSeamline(Ar),

            // Metro:Awakening
            "VGCoverDataPoint" => new VGCoverDataPoint(Ar),

            // Marvel Rivals
            "MarvelSoftObjectPath" => new FMarvelSoftObjectPath(Ar),

            // Wuthering Waves
            "VectorDouble" => type == ReadType.ZERO ? new TIntVector3<double>() : Ar.Read<TIntVector3<double>>(),

            // Gothic 1 Remake
            "WaynetNode" when Ar.Game == EGame.GAME_Gothic1Remake => new FWaynetNode(Ar),
            "WaynetPath" when Ar.Game == EGame.GAME_Gothic1Remake => new FWaynetPath(Ar),

            // Brickadia
            "BrickStudGroup" when Ar.Game == EGame.GAME_Brickadia => new FBrickStudGroup(Ar),
            "BRGuid" when Ar.Game == EGame.GAME_Brickadia => type == ReadType.ZERO ? new FGuid() : Ar.Read<FGuid>(),

            // Deadside
            "SoundAttenuationPluginSettingsWithOverride" => new FSoundAttenuationPluginSettingsWithOverride(Ar),

            // Tempest Rising
            "OffsetCoords" when Ar.Game == EGame.GAME_TempestRising => type == ReadType.ZERO ? new TIntVector2<float>() : Ar.Read<TIntVector2<float>>(),
            "TedInstancedStruct" => new FInstancedStruct(Ar),
            "TedMarkerHandle" or "FoWAgentHandle" => type == ReadType.ZERO ? new TIntVector1<int>() : Ar.Read<TIntVector1<int>>(),

            // Avowed
            "NiagaraUserParameterModifier" => new NiagaraUserParameterModifier(Ar),

            // State of Decay 2
            "ItemsBitArray" when Ar.Game == EGame.GAME_StateOfDecay2 => type == ReadType.ZERO ? new FItemsBitArray() : new FItemsBitArray(Ar),

            // Dune Awakening
            "BodyInstance" when Ar.Game == EGame.GAME_DuneAwakening => new FBodyInstance(Ar),
            "GenericTeamId" when Ar.Game == EGame.GAME_DuneAwakening => new FGenericTeamId(Ar),
            "UniqueID" when Ar.Game == EGame.GAME_DuneAwakening => new FUniqueID(Ar),
            "BotAutoBorderCrossingConfig" when Ar.Game == EGame.GAME_DuneAwakening => new FBotAutoBorderCrossingConfig(Ar),

            // MindsEye
            "UgcData" when Ar.Game == EGame.GAME_MindsEye => new FUgcData(Ar),
            "JsonObjectWrapper" when Ar.Game == EGame.GAME_MindsEye => new FJsonObjectWrapper(Ar),
            "UGCPropertyDefaultValueOverride" when Ar.Game == EGame.GAME_MindsEye => new FUGCPropertyDefaultValueOverride(Ar),

            // Vindictus Defying Fate
            "VinInstancedStruct" => new FInstancedStruct(Ar),
            "VinInstancedPropertyBag" => new FInstancedPropertyBag(Ar),
            "AnyValue" => new FAnyValue(Ar),

            // Strinova
            "EveryPlatformFloat" => new FEveryPlatformFloat(Ar),
            "EveryPlatformBool" => new FEveryPlatformBool(Ar),
            "EveryPlatformInt" => new FEveryPlatformInt(Ar),

            // Killing Floor 3
            "HavokAIAnyArray" => new FHavokAIAnyArray(Ar),

            "SUDSValue" => type == ReadType.ZERO ? new FStructFallback() : new FSUDSValue(Ar),

            _ => type == ReadType.ZERO ? new FStructFallback() : struc != null ? new FStructFallback(Ar, struc) : new FStructFallback(Ar, structName)
        };
    }

    public FScriptStruct(IUStruct structType)
    {
        StructType = structType;
    }

    public override string ToString() => $"{StructType} ({StructType.GetType().Name})";
}
