using System.Runtime.CompilerServices;
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
        };

        public static readonly FGuid GUID = new FGuid(0x29E575DD, 0xE0A34627, 0x9D10D276, 0x232CDCEA);
        
        public static Type Get(FAssetArchive Ar)
        {

            int ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type)ver;

            if (Ar.Game < EGame.GAME_UE4_16)
                return Type.BeforeCustomVersionWasAdded;
            if (Ar.Game < EGame.GAME_UE4_17)
                return (Type)3;
            if (Ar.Game < EGame.GAME_UE4_18)
                return (Type)7;
            if (Ar.Game < EGame.GAME_UE4_19)
                return Type.AddLODToCurveMetaData;
            if (Ar.Game < EGame.GAME_UE4_20)
                return (Type)16;
            if (Ar.Game < EGame.GAME_UE4_26)
                return (Type)17;

            return Type.LatestVersion;
        }
    }
}