using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Versions
{
    public static class FFrameworkObjectVersion
    {
        public enum Type
        {
            BeforeCustomVersionWasAdded = 0,
            UseBodySetupCollisionProfile,
            AnimBlueprintSubgraphFix,
            MeshSocketScaleUtilization,
            ExplicitAttachmentRules,
            MoveCompressedAnimDataToTheDDC,
            FixNonTransactionalPins,
            SmartNameRefactor,
            AddSourceReferenceSkeletonToRig,
            ConstraintInstanceBehaviorParameters,
            PoseAssetSupportPerBoneMask,
            PhysAssetUseSkeletalBodySetup,
            RemoveSoundWaveCompressionName,
            AddInternalClothingGraphicalSkinning,
            WheelOffsetIsFromWheel,
            MoveCurveTypesToSkeleton,
            CacheDestructibleOverlaps,
            GeometryCacheMissingMaterials,
            LODsUseResolutionIndependentScreenSize,
            BlendSpacePostLoadSnapToGrid,
            SupportBlendSpaceRateScale,
            LODHysteresisUseResolutionIndependentScreenSize,
            ChangeAudioComponentOverrideSubtitlePriorityDefault,
            HardSoundReferences,
            EnforceConstInAnimBlueprintFunctionGraphs,
            InputKeySelectorTextStyle,
            EdGraphPinContainerType,
            ChangeAssetPinsToString,
            LocalVariablesBlueprintVisible,
            RemoveUField_Next,
            UserDefinedStructsBlueprintVisible,
            PinsStoreFName,
            UserDefinedStructsStoreDefaultInstance,
            FunctionTerminatorNodesUseMemberReference,
            EditableEventsUseConstRefParameters,
            BlueprintGeneratedClassIsAlwaysAuthoritative,
            EnforceBlueprintFunctionVisibility,
            StoringUCSSerializationIndex,
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1,
        }

        public static readonly FGuid GUID = new(0xCFFC743F, 0x43B04480, 0x939114DF, 0x171D2073);

        public static Type Get(FAssetArchive Ar)
        {
            var ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_12 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_13 => (Type) 6,
                < EGame.GAME_UE4_14 => Type.RemoveSoundWaveCompressionName,
                < EGame.GAME_UE4_15 => Type.GeometryCacheMissingMaterials,
                < EGame.GAME_UE4_16 => (Type) 22,
                < EGame.GAME_UE4_17 => (Type) 23,
                < EGame.GAME_UE4_18 => (Type) 28,
                < EGame.GAME_UE4_19 => (Type) 30,
                < EGame.GAME_UE4_20 => (Type) 33,
                < EGame.GAME_UE4_22 => (Type) 34,
                < EGame.GAME_UE4_24 => (Type) 35,
                < EGame.GAME_UE4_25 => (Type) 36,
                < EGame.GAME_UE4_26 => (Type) 37,
                _ => Type.LatestVersion
            };
        }
    }
}