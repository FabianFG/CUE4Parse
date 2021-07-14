using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Versions
{
    public static class FAnimPhysObjectVersion
    {
        public enum Type
        {
            BeforeCustomVersionWasAdded,
            ConvertAnimNodeLookAtAxis,
            BoxSphylElemsUseRotators,
            ThumbnailSceneInfoAndAssetImportDataAreTransactional,
            AddedClothingMaskWorkflow,
            RemoveUIDFromSmartNameSerialize,
            CreateTargetReference,
            TuneSoftLimitStiffnessAndDamping,
            FixInvalidClothParticleMasses,
            CacheClothMeshInfluences,
            SmartNameRefactorForDeterministicCooking,
            RenameDisableAnimCurvesToAllowAnimCurveEvaluation,
            AddLODToCurveMetaData,
            FixupBadBlendProfileReferences,
            AllowMultipleAudioPluginSettings,
            ChangeRetargetSourceReferenceToSoftObjectPtr,
            SaveEditorOnlyFullPoseForPoseAsset,
            GeometryCacheAssetDeprecation,
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1,
        }

        public static readonly FGuid GUID = new(0x29E575DD, 0xE0A34627, 0x9D10D276, 0x232CDCEA);

        public static Type Get(FAssetArchive Ar)
        {
            var ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_16 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_17 => (Type) 3,
                < EGame.GAME_UE4_18 => (Type) 7,
                < EGame.GAME_UE4_19 => Type.AddLODToCurveMetaData,
                < EGame.GAME_UE4_20 => (Type) 16,
                < EGame.GAME_UE4_26 => (Type) 17,
                _ => Type.LatestVersion
            };
        }
    }
}