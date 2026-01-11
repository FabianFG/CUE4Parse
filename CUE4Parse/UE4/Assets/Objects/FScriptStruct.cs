using System;
using CUE4Parse.GameTypes._2XKO.Assets.Exports;
using CUE4Parse.GameTypes.Borderlands4.Assets.Objects;
using CUE4Parse.GameTypes.Brickadia.Objects;
using CUE4Parse.GameTypes.DuneAwakening.Assets.Objects;
using CUE4Parse.GameTypes.FN.Objects;
using CUE4Parse.GameTypes.Gothic1R.Assets.Objects;
using CUE4Parse.GameTypes.L2KD.Objects;
using CUE4Parse.GameTypes.MA.Objects;
using CUE4Parse.GameTypes.MindsEye.Objects;
using CUE4Parse.GameTypes.NetEase.MAR.Objects;
using CUE4Parse.GameTypes.OtherGames.Objects;
using CUE4Parse.GameTypes.OuterWorlds2.Objects;
using CUE4Parse.GameTypes.PUBG.Assets.Objects;
using CUE4Parse.GameTypes.SG2.Objects;
using CUE4Parse.GameTypes.SOD2.Assets.Objects;
using CUE4Parse.GameTypes.SuicideSquad.Objects;
using CUE4Parse.GameTypes.SWJS.Objects;
using CUE4Parse.GameTypes.TL.Objects;
using CUE4Parse.GameTypes.TQ2.Objects;
using CUE4Parse.GameTypes.TSW.Objects;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.Harmonix;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.ChaosCaching;
using CUE4Parse.UE4.Objects.ControlRig;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Ai;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.Engine.ComputeFramework;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.Engine.EdGraph;
using CUE4Parse.UE4.Objects.Engine.GameFramework;
using CUE4Parse.UE4.Objects.Engine.Material;
using CUE4Parse.UE4.Objects.Engine.Midi;
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
using FRawUIntStruct = CUE4Parse.UE4.Objects.StructUtils.FRawStruct<uint>;

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
            "Box3f" => type == ReadType.ZERO ? new TBox3<float>() : new TBox3<float>(Ar),
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
            "PerPlatformBool" => type == ReadType.ZERO ? new FPerPlatformBool() : new FPerPlatformBool(Ar),
            "PerPlatformFloat" => type == ReadType.ZERO ? new FPerPlatformFloat() : new FPerPlatformFloat(Ar),
            "PerPlatformInt" => type == ReadType.ZERO ? new FPerPlatformInt() : new FPerPlatformInt(Ar),
            "PerPlatformFrameRate" => type == ReadType.ZERO ? new FPerPlatformFrameRate() : new FPerPlatformFrameRate(Ar),
            "PerPlatformFString" => type == ReadType.ZERO ? new FPerPlatformFString() : new FPerPlatformFString(Ar),
            "PerQualityLevelInt" => type == ReadType.ZERO ? new FPerQualityLevelInt() : new FPerQualityLevelInt(Ar),
            "PerQualityLevelFloat" => type == ReadType.ZERO ? new FPerQualityLevelFloat() : new FPerQualityLevelFloat(Ar),
            "PannerDetails" => new FPannerDetails(Ar),
            "GameplayTagContainer" => type == ReadType.ZERO ? new FGameplayTagContainer() : new FGameplayTagContainer(Ar),
            "IntPoint" or "Int32Point" => type == ReadType.ZERO ? new FIntPoint() : Ar.Read<FIntPoint>(),
            "IntVector2" => type == ReadType.ZERO ? new TIntVector2<int>() : Ar.Read<TIntVector2<int>>(),
            "UintVector2" or "Uint32Point" => type == ReadType.ZERO ? new TIntVector2<uint>() : Ar.Read<TIntVector2<uint>>(),
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
            "MovieSceneTrackIdentifiers" when Ar.Game is EGame.GAME_GameForPeace => type == ReadType.ZERO ? new FMovieSceneTrackIdentifiers() : new FMovieSceneTrackIdentifiers(Ar),
            "MovieSceneTrackImplementationPtr" => new FMovieSceneTrackImplementationPtr(Ar),
            "MidiEvent" => new FMidiEvent(Ar),
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
            "Spline" => type == ReadType.ZERO ? new FSpline() : new FSpline(Ar),
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
            "TypedParameter" => new FTypedParameter(Ar),
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
            "InstancedOverridablePropertyBag" => new FInstancedOverridablePropertyBag(Ar),
            "WorldConditionQueryDefinition" => new FWorldConditionQueryDefinition(Ar),
            "UniversalObjectLocatorFragment" => type == ReadType.ZERO ? new FUniversalObjectLocatorFragment() : new FUniversalObjectLocatorFragment(Ar),
            "KeyHandleMap" => new FStructFallback(),
            "ShaderValueTypeHandle" => new FShaderValueTypeHandle(Ar),
            "AnimationAttributeIdentifier" => new FAnimationAttributeIdentifier(Ar),
            "AttributeCurve" => new FAttributeCurve(Ar),
            "PCGPoint" => FFortniteReleaseBranchCustomObjectVersion.Get(Ar) < FFortniteReleaseBranchCustomObjectVersion.Type.PCGPointStructuredSerializer ? new FStructFallback(Ar, "PCGPoint") : new FPCGPoint(Ar),
            "PCGDataPtrWrapper" => new FPCGDataPtrWrapper(Ar),
            "PCGPointArray" => new FPCGPointArray(Ar),
            "CacheEventTrack" => type == ReadType.ZERO ? new FStructFallback() : new FCacheEventTrack(Ar),
            "StateTreeInstanceData" => type == ReadType.ZERO ? new FStructFallback() : new FStateTreeInstanceData(Ar),
            "DataCacheDuplicatedObjectData" => new FDataCacheDuplicatedObjectData(Ar),
            "EdGraphPinType" => new FEdGraphPinType(Ar),
            "ControlRigOverrideContainer" => type == ReadType.ZERO ? new FControlRigOverrideContainer() : new FControlRigOverrideContainer(Ar),

            // Custom struct types for simple structs in tagged TMap
            "UInt32Property" => type == ReadType.ZERO ? new FRawUIntStruct() : Ar.Read<FRawUIntStruct>(),
            // Custom struct type to skip unknown properties
            "ZeroSizeProperty" => new FRawUIntStruct(),

            // FortniteGame
            "ConnectivityCube" => new FConnectivityCube(Ar),
            "FortActorRecord" => new FFortActorRecord(Ar),
            "GameplayEventFunction" => new FGameplayEventFunction(Ar),

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
            "TLJsonGuid" => Ar.Read<FTLJsonGuid>(),
            "TLJsonVector" => type == ReadType.ZERO ? new FVector() : new FVector(Ar),
            "TLJsonVector2D" => type == ReadType.ZERO ? new FVector2D() : new FVector2D(Ar),
            "SceneFaceDefSeamline" => new FSceneFaceDefSeamline(Ar),

            // Metro:Awakening
            "VGCoverDataPoint" => new VGCoverDataPoint(Ar),

            // Marvel Rivals
            "MarvelSoftObjectPath" => new FMarvelSoftObjectPath(Ar),

            // Wuthering Waves
            "VectorDouble" => type == ReadType.ZERO ? new TIntVector3<double>() : Ar.Read<TIntVector3<double>>(),

            "WaynetNode" when Ar.Game == EGame.GAME_Gothic1Remake => new FWaynetNode(Ar),
            "WaynetPath" when Ar.Game == EGame.GAME_Gothic1Remake => new FWaynetPath(Ar),

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

            "ItemsBitArray" when Ar.Game == EGame.GAME_StateOfDecay2 => type == ReadType.ZERO ? new FItemsBitArray() : new FItemsBitArray(Ar),

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

            // Upin&Ipin Universe
            "SUDSValue" => type == ReadType.ZERO ? new FStructFallback() : new FSUDSValue(Ar),

            "SimpleBox" when Ar.Game == EGame.GAME_SuicideSquad => Ar.Read<FSimpleBox>(),
            "RRotationTranslation" when Ar.Game == EGame.GAME_SuicideSquad => Ar.Read<FRRotationTranslation>(),

            // Concord
            "PMTimelineSectionFloatMapping" or "PMTimelineSectionEventFloatMapping" => new FPMFloatMapping(Ar),
            "PMFloatMapping" or "PMBoundFloatMapping" or "FWAbilityResourceFloatMapping" => new FPMFloatMapping(Ar),
            "GameplayEffectFloatMapping" or "PMTimelineFloatMapping" or "FWPerkContextFloatMapping" => new FPMFloatMapping(Ar),
            "PMTimelineObjectBindingDef" => new FPMTimelineObjectBindingDef(Ar),
            "GameplayEffectApplicationDataHandle" => new FGameplayEffectApplicationDataHandle(Ar),
            "PMTimelineRelevancy" => new FPMTimelineRelevancy(Ar),

            // Lost Soul Aside
            "LSAAudioSectionkey" => new FStructFallback(Ar, structName, FRawHeader.FullRead, ReadType.RAW),

            // 2XKO
            "FixedPoint" => new FFixedPoint(Ar),

            // PUBG
            "TslSomeSKStruct" => new FTslSomeSKStruct(Ar),

            // Train Sim World 6
            "PowerQuantity" or "ForceQuantity" or "TimeUnit" or "PressureQuantity" or "VolumeQuantity" => new FStructFallback(Ar, structName, FRawHeader.FullRead),

            // Daimon Blades
            "SOS_GDValue" => new FStructFallback(Ar, structName, new FRawHeader([(1, 1)]), ReadType.RAW),

            "SoftEnumName" when Ar.Game is EGame.GAME_LittleNightmares3 => new FStructFallback(Ar, structName, new FRawHeader([(1, 1)]), ReadType.RAW),
            "KosmosHangTraversalData" when Ar.Game is EGame.GAME_LittleNightmares3 => new FStructFallback(Ar, structName, FRawHeader.FullRead, ReadType.RAW),

            "EnumName" when Ar.Game is EGame.GAME_Reanimal => new FStructFallback(Ar, structName, new FRawHeader([(1, 1)]), ReadType.RAW),
            "AbstractEnum" or "EnumName" or "JumpParams" when Ar.Game is EGame.GAME_Reanimal => new FStructFallback(Ar, structName, FRawHeader.FullRead, ReadType.RAW),
            "ChalkboardSectionKey" when Ar.Game is EGame.GAME_Reanimal => new FStructFallback(Ar, structName, new FRawHeader([(0, 2), (2, 2)]), ReadType.RAW),
            "LedgeMetaData" when Ar.Game is EGame.GAME_Reanimal => new FFixedSizeStruct(Ar, 1),

            "ItemDataContainer" when Ar.Game is EGame.GAME_VEIN => new FStructFallback(Ar, structName, FRawHeader.FullRead, ReadType.RAW),

            //SpongeBob SquarePants: Titans of the Tide
            "GG_CrowdGeneratorLocations" => new FStructFallback(Ar, structName, FRawHeader.FullRead, ReadType.RAW),

            "Linecode" when Ar.Game is EGame.GAME_Psychonauts2 => new FLinecode(Ar),
            "P2Attribute" when Ar.Game is EGame.GAME_Psychonauts2 => new FP2Attribute(Ar),

            "AIBehaviorTreeReference" when Ar.Game is EGame.GAME_OuterWorlds2 => new FAIBehaviorTreeReference(Ar),
            "BlueprintFunctionLibraryConditional" when Ar.Game is EGame.GAME_OuterWorlds2 => new FBlueprintFunctionLibraryConditional(Ar, true),
            "BlueprintDefinedScript" when Ar.Game is EGame.GAME_OuterWorlds2 => new FBlueprintFunctionLibraryConditional(Ar, false),
            "AIGroupBehaviorClassSelector" when Ar.Game is EGame.GAME_OuterWorlds2 => new FSoftObjectPath(Ar),

            // The Last Caretaker 
            "VoyagePackedLocalTransform" => new VoyagePackedLocalTransform(Ar),
            "VoyageFloat16" => Ar.Read<FRawStruct<Half>>(),

            "EncVector" when Ar.Game is EGame.GAME_DeltaForceHawkOps => Ar.Read<FVector>(),

            "MercunaUsageSpec" when Ar.Game is EGame.GAME_PUBGBlackBudget => new FStructFallback(Ar, structName, FRawHeader.FullRead, ReadType.RAW),
            "MercunaUsageTypes" when Ar.Game is EGame.GAME_PUBGBlackBudget => type == ReadType.ZERO ? new FRawUIntStruct() : Ar.Read<FRawUIntStruct>(),

            // Palia
            "VAL_CharacterCustomizationVariantOptionsArray" => new FVAL_CharacterCustomizationVariantOptionsArray(Ar),

            _ => Ar.Game switch
            {
                EGame.GAME_TitanQuest2 => TQ2Structs.ParseTQ2Struct(Ar, structName, struc, type),
                EGame.GAME_DuneAwakening => DAStructs.ParseDAStruct(Ar, structName, struc, type),
                EGame.GAME_Borderlands4 => Borderlands4Structs.ParseBl4Struct(Ar, structName, struc, type),
                _ when type == ReadType.RAW => new FStructFallback(Ar, structName, FRawHeader.FullRead, ReadType.RAW),
                _ => type == ReadType.ZERO ? new FStructFallback() : struc != null ? new FStructFallback(Ar, struc) : new FStructFallback(Ar, structName)
            },
        };
    }

    public FScriptStruct(IUStruct structType)
    {
        StructType = structType;
    }

    public override string ToString() => $"{StructType} ({StructType.GetType().Name})";
}
