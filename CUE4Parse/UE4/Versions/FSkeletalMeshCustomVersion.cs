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
            UseSharedColorBufferFormat = 6, // separate vertex stream for vertex influences
            UseSeparateSkinWeightBuffer = 7, // use FColorVertexStream for both static and skeletal meshes
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
            SkinWeightProfiles = 17, // TODO: FSkeletalMeshLODModel::Serialize (editor mesh)
            // UE4.26 = 18
            RemoveEnableClothLOD = 18, // TODO
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        };

        public static readonly FGuid GUID = new(0xD78A4A00, 0xE8584697, 0xBAA819B5, 0x487D46B4);

        public static Type Get(FAssetArchive Ar)
        {
            var ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_13 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_14 => Type.SaveNumVertices,
                < EGame.GAME_UE4_15 => (Type) 5,
                < EGame.GAME_UE4_16 => Type.UseSeparateSkinWeightBuffer,
                < EGame.GAME_UE4_18 => (Type) 9,
                < EGame.GAME_UE4_19 => Type.CompactClothVertexBuffer,
                < EGame.GAME_UE4_20 => Type.DeprecateSectionDisabledFlag,
                < EGame.GAME_UE4_23 => Type.SectionIgnoreByReduceAdded,
                < EGame.GAME_UE4_26 => Type.SkinWeightProfiles,
                _ => Type.RemoveEnableClothLOD
            };
        }
    }
}