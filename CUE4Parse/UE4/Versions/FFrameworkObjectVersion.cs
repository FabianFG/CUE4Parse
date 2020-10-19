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
        };

        public static readonly FGuid GUID = new FGuid(0xCFFC743F, 0x43B04480, 0x939114DF, 0x171D2073);
        
        public static Type Get(FAssetArchive Ar)
        {
            int ver = VersionUtils.GetUE4CustomVersion(Ar.Owner.Summary, GUID);
            if (ver >= 0)
                return (Type)ver;

            if (Ar.Game < EGame.GAME_UE4_12)
                return Type.BeforeCustomVersionWasAdded;
            if (Ar.Game < EGame.GAME_UE4_13)
                return (Type)6;
            if (Ar.Game < EGame.GAME_UE4_14)
                return Type.RemoveSoundWaveCompressionName;
            if (Ar.Game < EGame.GAME_UE4_15)
                return Type.GeometryCacheMissingMaterials;
            if (Ar.Game < EGame.GAME_UE4_16)
                return (Type)22;
            if (Ar.Game < EGame.GAME_UE4_17)
                return (Type)23;
            if (Ar.Game < EGame.GAME_UE4_18)
                return (Type)28;
            if (Ar.Game < EGame.GAME_UE4_19)
                return (Type)30;
            if (Ar.Game < EGame.GAME_UE4_20)
                return (Type)33;
            if (Ar.Game < EGame.GAME_UE4_22)
                return (Type)34;
            if (Ar.Game < EGame.GAME_UE4_24)
                return (Type)35;
            if (Ar.Game < EGame.GAME_UE4_25)
                return (Type)36;
            if (Ar.Game < EGame.GAME_UE4_26)
                return (Type)37;

            return Type.LatestVersion;
        }
    }
}