using System;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public class FMaterial
    {
        public void DeserializeInlineShaderMap(FMaterialResourceProxyReader Ar)
        {
            var cooked = Ar.ReadBoolean();
            //check(cooked)
            var valid = Ar.ReadBoolean();
            //check(valid)
            var loadedShaderMap = new FMaterialShaderMap();
            loadedShaderMap.Deserialize(Ar);
        }
    }

    public class FMaterialResource : FMaterial { }

    // MaterialShader.cpp
    public class FMaterialShaderMap : FShaderMapBase
    {
        public FMaterialShaderMapId ShaderMapId;

        public void Deserialize(FMaterialResourceProxyReader Ar)
        {
            ShaderMapId = new FMaterialShaderMapId(Ar);
            base.Deserialize(Ar);
        }
    }

    public struct FHashedName
    {
        public ulong Hash;
    }

    // Shader.h
    public class FShaderMapBase
    {
        public void Deserialize(FMaterialResourceProxyReader Ar)
        {
            var bUseNewFormat = Ar.Versions["ShaderMap.UseNewCookedFormat"];
            #region FMemoryImageResult::LoadFromArchive, MemoryImage.cpp
            if (bUseNewFormat)
            {
                var layoutParameters = Ar.Read<FPlatformTypeLayoutParameters>();
            }

            var frozenSize = Ar.Read<int>();
            var frozenObject = Ar.ReadBytes(frozenSize);

            if (bUseNewFormat)
            {
                //var bFrozenObjectIsValid = pointerTable.LoadFromArchive(Ar, layoutParameters, frozenObject);
                FShaderMapPointerTable_LoadFromArchive(Ar, bUseNewFormat);
            }

            var numVTables = Ar.Read<uint>();
            var numScriptNames = Ar.Read<uint>();
            var numMinimalNames = Ar.Read<uint>();

            for (var i = 0; i < numVTables; i++)
            {
                var typeNameHash = Ar.Read<ulong>();
                var numPatches = Ar.Read<uint>();

                for (var patchIndex = 0; patchIndex < numPatches; ++patchIndex)
                {
                    var vTableOffset = Ar.Read<uint>();
                    var offset = Ar.Read<uint>();
                }
            }

            for (var i = 0; i < numScriptNames; i++)
            {
                var name = Ar.ReadFName();
                var numPatches = Ar.Read<uint>();

                for (var patchIndex = 0; patchIndex < numPatches; ++patchIndex)
                {
                    var offset = Ar.Read<uint>();
                }
            }

            for (var i = 0; i < numMinimalNames; i++)
            {
                var name = Ar.ReadFName();
                var numPatches = Ar.Read<uint>();

                for (var patchIndex = 0; patchIndex < numPatches; ++patchIndex)
                {
                    var offset = Ar.Read<uint>();
                }
            }

            #endregion

            if (!bUseNewFormat)
            {
                FShaderMapPointerTable_LoadFromArchive(Ar, bUseNewFormat);
            }

            var bShareCode = Ar.ReadBoolean();
            if (bUseNewFormat)
            {
                var shaderPlatform = Ar.Read<byte>();
            }

            if (bShareCode)
            {
                var resourceHash = new FSHAHash(Ar);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        // Shader.h
        private static void FShaderMapPointerTable_LoadFromArchive(FMaterialResourceProxyReader Ar, bool bUseNewFormat)
        {
            if (bUseNewFormat)
            {
                FPointerTableBase_LoadFromArchive(Ar);
            }

            var numTypes = Ar.Read<int>();
            var numVFTypes = Ar.Read<int>();

            for (var typeIndex = 0; typeIndex < numTypes; ++typeIndex)
            {
                var typeName = Ar.Read<FHashedName>();
            }

            for (var vfTypeIndex = 0; vfTypeIndex < numVFTypes; ++vfTypeIndex)
            {
                var vfTypeName = Ar.Read<FHashedName>();
            }

            if (!bUseNewFormat)
            {
                FPointerTableBase_LoadFromArchive(Ar);
            }
        }

        // MemoryImage.cpp
        private static void FPointerTableBase_LoadFromArchive(FMaterialResourceProxyReader Ar)
        {
            var numDependencies = Ar.Read<int>();
            for (var i = 0; i < numDependencies; ++i)
            {
                var nameHash = Ar.Read<ulong>();
                var savedLayoutSize = Ar.Read<uint>();
                var savedLayoutHash = new FSHAHash(Ar);
            }
        }
    }

    // MemoryImage.cpp
    public class FPointerTableBase { }

    public class FShaderMapPointerTable { }

    public class TShaderMap<ContentType, PointerTableType> : FShaderMapBase { }

    // MemoryLayout.h
    public struct FPlatformTypeLayoutParameters
    {
        public uint MaxFieldAlignment;
        public EFlags Flags;

        [Flags]
        public enum EFlags : uint
        {
            Flag_Initialized = (1 << 0),
            Flag_Is32Bit = (1 << 1),
            Flag_AlignBases = (1 << 2),
            Flag_WithEditorOnly = (1 << 3),
            Flag_WithRaytracing = (1 << 4),
        }
    }

    /** Contains all the information needed to uniquely identify a FMaterialShaderMap. */
    public class FMaterialShaderMapId
    {
        public FSHAHash CookedShaderMapIdHash;

        /** 
	     * Quality level that this shader map is going to be compiled at.  
	     * Can be a value of EMaterialQualityLevel::Num if quality level doesn't matter to the compiled result.
	     */
        public EMaterialQualityLevel QualityLevel;

        /** Feature level that the shader map is going to be compiled for. */
        public ERHIFeatureLevel FeatureLevel;

        /** Type layout parameters of the memory image */
        public FPlatformTypeLayoutParameters LayoutParams;

        public FMaterialShaderMapId(FMaterialResourceProxyReader Ar)
        {
            var bIsLegacyPackage = Ar.Ver < (UE4Version) 260;
            if (!bIsLegacyPackage)
            {
                QualityLevel = (EMaterialQualityLevel) Ar.Read<int>();
                FeatureLevel = (ERHIFeatureLevel) Ar.Read<int>();
            }
            else
            {
                var legacyQualityLevel = Ar.Read<byte>();
            }

            // Cooked so can assume this is valid
            CookedShaderMapIdHash = new FSHAHash(Ar);

            if (!bIsLegacyPackage)
            {
                LayoutParams = Ar.Read<FPlatformTypeLayoutParameters>();
            }
        }
    }

    // SceneTypes.h
    /** Quality levels that a material can be compiled for. */
    public enum EMaterialQualityLevel : byte
    {
        Low,
        High,
        Medium,
        Epic,
        Num
    }

    // RHIDefinitions.h
    /**
     * The RHI's feature level indicates what level of support can be relied upon.
     * Note: these are named after graphics API's like ES3 but a feature level can be used with a different API (eg ERHIFeatureLevel::ES3.1 on D3D11)
     * As long as the graphics API supports all the features of the feature level (eg no ERHIFeatureLevel::SM5 on OpenGL ES3.1)
     */
    public enum ERHIFeatureLevel : byte
    {
        /** Feature level defined by the core capabilities of OpenGL ES2. Deprecated */
        ES2_REMOVED,

        /** Feature level defined by the core capabilities of OpenGL ES3.1 & Metal/Vulkan. */
        ES3_1,

        /**
         * Feature level defined by the capabilities of DX10 Shader Model 4.
         * SUPPORT FOR THIS FEATURE LEVEL HAS BEEN ENTIRELY REMOVED.
         */
        SM4_REMOVED,

        /**
         * Feature level defined by the capabilities of DX11 Shader Model 5.
         *   Compute shaders with shared memory, group sync, UAV writes, integer atomics
         *   Indirect drawing
         *   Pixel shaders with UAV writes
         *   Cubemap arrays
         *   Read-only depth or stencil views (eg read depth buffer as SRV while depth test and stencil write)
         * Tessellation is not considered part of Feature Level SM5 and has a separate capability flag.
         */
        SM5,

        /**
         * Feature level defined by the capabilities of DirectX 12 hardware feature level 12_2 with Shader Model 6.5
         *   Raytracing Tier 1.1
         *   Mesh and Amplification shaders
         *   Variable rate shading
         *   Sampler feedback
         *   Resource binding tier 3
         */
        SM6,

        Num
    }
}