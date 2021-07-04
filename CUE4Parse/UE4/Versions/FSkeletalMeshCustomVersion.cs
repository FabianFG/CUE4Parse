using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Versions
{
    public static class FSkeletalMeshCustomVersion
    {
        public enum Type
        {
            BeforeCustomVersionWasAdded = 0,
            // UE4.13 = 4
            CombineSectionWithChunk = 1,
            CombineSoftAndRigidVerts = 2,
            RecalcMaxBoneInfluences = 3,
            SaveNumVertices = 4,
            // UE4.14 = 5
            // UE4.15 = 7
            UseSharedColorBufferFormat = 6,		// separate vertex stream for vertex influences
            UseSeparateSkinWeightBuffer = 7,	// use FColorVertexStream for both static and skeletal meshes
            // UE4.16, UE4.17 = 9
            NewClothingSystemAdded = 8,
            // UE4.18 = 10
            CompactClothVertexBuffer = 10,
            // UE4.19 = 15
            RemoveSourceData = 11,
            SplitModelAndRenderData = 12,
            RemoveTriangleSorting = 13,
            RemoveDuplicatedClothingSections = 14,
            DeprecateSectionDisabledFlag = 15,
            // UE4.20-UE4.22 = 16
            SectionIgnoreByReduceAdded = 16,
            // UE4.23-UE4.25 = 17
            SkinWeightProfiles = 17, //todo: FSkeletalMeshLODModel::Serialize (editor mesh)
            // UE4.26 = 18
            RemoveEnableClothLOD = 18, //todo

            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        };
        
        public static readonly FGuid GUID = new FGuid(0xD78A4A00, 0xE8584697, 0xBAA819B5, 0x487D46B4);
        
        public static Type Get(FAssetArchive Ar)
        {
            int ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type)ver;

            if (Ar.Game < EGame.GAME_UE4_13)
                return Type.BeforeCustomVersionWasAdded;
            if (Ar.Game < EGame.GAME_UE4_14)
                return Type.SaveNumVertices;
            if (Ar.Game < EGame.GAME_UE4_15)
                return (Type)5;
            if (Ar.Game < EGame.GAME_UE4_16)
                return Type.UseSeparateSkinWeightBuffer;
            if (Ar.Game < EGame.GAME_UE4_18)
                return (Type)9;
            if (Ar.Game < EGame.GAME_UE4_19)
                return Type.CompactClothVertexBuffer;
            if (Ar.Game < EGame.GAME_UE4_20)
                return Type.DeprecateSectionDisabledFlag;
            if (Ar.Game < EGame.GAME_UE4_23)
                return Type.SectionIgnoreByReduceAdded;
            if (Ar.Game < EGame.GAME_UE4_26)
                return Type.SkinWeightProfiles;
//		if (Ar.Game < GAME_UE4(27))
            return Type.RemoveEnableClothLOD;
            // NEW_ENGINE_VERSION
//		return LatestVersion;
        }
    }
}