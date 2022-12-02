using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    // Custom serialization version for SkeletalMesh types
    public static class FSkeletalMeshCustomVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded = 0,
            // UE4.13 = 4
            // Remove Chunks array in FStaticLODModel and combine with Sections array
            CombineSectionWithChunk = 1,
            // Remove FRigidSkinVertex and combine with FSoftSkinVertex array
            CombineSoftAndRigidVerts = 2,
            // Need to recalc max bone influences
            RecalcMaxBoneInfluences = 3,
            // Add NumVertices that can be accessed when stripping editor data
            SaveNumVertices = 4,
            // UE4.14 = 5
            // Regenerated clothing section shadow flags from source sections
            RegenerateClothingShadowFlags = 5,
            // UE4.15 = 7
            // Share color buffer structure with StaticMesh
            UseSharedColorBufferFormat = 6,
            // Use separate buffer for skin weights
            UseSeparateSkinWeightBuffer = 7,
            // UE4.16, UE4.17 = 9
            // Added new clothing systems
            NewClothingSystemAdded = 8,
            // Cached inv mass data for clothing assets
            CachedClothInverseMasses = 9,
            // UE4.18 = 10
            // Compact cloth vertex buffer, without dummy entries
            CompactClothVertexBuffer = 10,
            // UE4.19 = 15
            // Remove SourceData
            RemoveSourceData = 11,
            // Split data into Model and RenderData
            SplitModelAndRenderData = 12,
            // Remove triangle sorting support
            RemoveTriangleSorting = 13,
            // Remove the duplicated clothing sections that were a legacy holdover from when we didn't use our own render data
            RemoveDuplicatedClothingSections = 14,
            // Remove 'Disabled' flag from SkelMesh asset sections
            DeprecateSectionDisabledFlag = 15,
            // UE4.20-UE4.22 = 16
            // Add Section ignore by reduce
            SectionIgnoreByReduceAdded = 16,
            // UE4.23-UE4.25 = 17
            // Adding skin weight profile support
            SkinWeightProfiles = 17, // TODO: FSkeletalMeshLODModel::Serialize (editor mesh)
            // UE4.26 = 18
            // Remove uninitialized/deprecated enable cloth LOD flag
            RemoveEnableClothLOD = 18, // TODO

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        public static readonly FGuid GUID = new(0xD78A4A00, 0xE8584697, 0xBAA819B5, 0x487D46B4);

        // TODO: This has been moved to "Legacy" in UE5
        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                // Game Overrides
                EGame.GAME_Paragon => Type.SplitModelAndRenderData,

                // Engine
                < EGame.GAME_UE4_13 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_14 => Type.SaveNumVertices,
                < EGame.GAME_UE4_15 => Type.RegenerateClothingShadowFlags,
                < EGame.GAME_UE4_16 => Type.UseSeparateSkinWeightBuffer,
                < EGame.GAME_UE4_18 => Type.CachedClothInverseMasses,
                < EGame.GAME_UE4_19 => Type.CompactClothVertexBuffer,
                < EGame.GAME_UE4_20 => Type.DeprecateSectionDisabledFlag,
                < EGame.GAME_UE4_23 => Type.SectionIgnoreByReduceAdded,
                < EGame.GAME_UE4_26 => Type.SkinWeightProfiles,
                _ => Type.RemoveEnableClothLOD
            };
        }
    }
}