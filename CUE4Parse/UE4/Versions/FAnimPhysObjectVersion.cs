using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    // Custom serialization version for changes made in Dev-AnimPhys stream
    public static class FAnimPhysObjectVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded,
            // convert animnode look at to use just default axis instead of enum, which doesn't do much
            ConvertAnimNodeLookAtAxis,
            // Change FKSphylElem and FKBoxElem to use Rotators not Quats for easier editing
            BoxSphylElemsUseRotators,
            // Change thumbnail scene info and asset import data to be transactional
            ThumbnailSceneInfoAndAssetImportDataAreTransactional,
            // Enabled clothing masks rather than painting parameters directly
            AddedClothingMaskWorkflow,
            // Remove UID from smart name serialize, it just breaks determinism 
            RemoveUIDFromSmartNameSerialize,
            // Convert FName Socket to FSocketReference and added TargetReference that support bone and socket
            CreateTargetReference,
            // Tune soft limit stiffness and damping coefficients
            TuneSoftLimitStiffnessAndDamping,
            // Fix possible inf/nans in clothing particle masses
            FixInvalidClothParticleMasses,
            // Moved influence count to cached data
            CacheClothMeshInfluences,
            // Remove GUID from Smart Names entirely + remove automatic name fixup
            SmartNameRefactorForDeterministicCooking,
            // rename the variable and allow individual curves to be set
            RenameDisableAnimCurvesToAllowAnimCurveEvaluation,
            // link curve to LOD, so curve metadata has to include LODIndex
            AddLODToCurveMetaData,
            // Fixed blend profile references persisting after paste when they aren't compatible
            FixupBadBlendProfileReferences,
            // Allowing multiple audio plugin settings
            AllowMultipleAudioPluginSettings,
            // Change RetargetSource reference to SoftObjectPtr
            ChangeRetargetSourceReferenceToSoftObjectPtr,
            // Save editor only full pose for pose asset 
            SaveEditorOnlyFullPoseForPoseAsset,
            // Asset change and cleanup to facilitate new streaming system
            GeometryCacheAssetDeprecation,
            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1,
        }

        public static readonly FGuid GUID = new(0x29E575DD, 0xE0A34627, 0x9D10D276, 0x232CDCEA);

        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_16 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_17 => Type.ThumbnailSceneInfoAndAssetImportDataAreTransactional,
                < EGame.GAME_UE4_18 => Type.TuneSoftLimitStiffnessAndDamping,
                < EGame.GAME_UE4_19 => Type.AddLODToCurveMetaData,
                < EGame.GAME_UE4_20 => Type.SaveEditorOnlyFullPoseForPoseAsset,
                < EGame.GAME_UE4_26 => Type.GeometryCacheAssetDeprecation,
                _ => Type.LatestVersion
            };
        }
    }
}