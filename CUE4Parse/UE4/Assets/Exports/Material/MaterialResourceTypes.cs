using System;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public class FMaterialResource : FMaterial
    {
    }

    public class FMaterial
    {
        public FMaterialShaderMap LoadedShaderMap;

        public void DeserializeInlineShaderMap(FMaterialResourceProxyReader Ar)
        {
            var bCooked = Ar.ReadBoolean();

            if (bCooked)
            {
                var bValid = Ar.ReadBoolean();

                if (bValid)
                {
                    var shaderMap = new FMaterialShaderMap();
                    shaderMap.Deserialize(Ar);
                    LoadedShaderMap = shaderMap;
                }
            }
        }
    }

    public class FShaderMapBase
    {
        public FMemoryImageResult ImageResult;
        public FSHAHash ResourceHash;
        public FShaderMapResourceCode Code;

        public void Deserialize(FMaterialResourceProxyReader Ar)
        {
            var bUseNewFormat = Ar.Versions["ShaderMap.UseNewCookedFormat"];

            ImageResult = new FMemoryImageResult(Ar);

            var bShareCode = Ar.ReadBoolean();
            if (bUseNewFormat)
            {
                var ShaderPlatform = Ar.Read<EShaderPlatform>();
            }

            if (bShareCode)
            {
                ResourceHash = new FSHAHash(Ar);
            }
            else
            {
                Code = new FShaderMapResourceCode(Ar);
            }
        }
    }

    public class FShaderMapResourceCode
    {
        public FSHAHash ResourceHash;
        public FSHAHash[] ShaderHashes;
        public FShaderEntry[] ShaderEntries;

        public FShaderMapResourceCode(FArchive Ar)
        {
            ResourceHash = new FSHAHash(Ar);
            ShaderHashes = Ar.ReadArray(() => new FSHAHash(Ar));
            ShaderEntries = Ar.ReadArray(() => new FShaderEntry(Ar));
        }
    }

    public class FShaderEntry
    {
        public byte[] Code; // Don't Serialize
        public int UncompressedSize;
        public byte Frequency; // Enum

        public FShaderEntry(FArchive Ar)
        {
            Code = Ar.ReadArray<byte>();
            UncompressedSize = Ar.Read<int>();
            Frequency = Ar.Read<byte>();
        }
    }

    public class FMemoryImageResult
    {
        public FPlatformTypeLayoutParameters? LayoutParameters;
        public FShaderMapPointerTable? ShaderMapPointerTable;
        public FPointerTableBase? PointerTable;

        public FMemoryImageResult(FArchive Ar) // LoadFromArchive
        {
            var bUseNewFormat = Ar.Versions["ShaderMap.UseNewCookedFormat"];

            if (bUseNewFormat)
                LayoutParameters = new FPlatformTypeLayoutParameters(Ar);

            var FrozenSize = Ar.Read<uint>();
            var FrozenObject = Ar.ReadBytes((int) FrozenSize);

            if (bUseNewFormat)
                PointerTable = new FPointerTableBase(Ar);

            var NumVTables = Ar.Read<uint>();
            var NumScriptNames = Ar.Read<uint>();
            var NumMinimalNames = Ar.Read<uint>();

            for (var i = 0; i < NumVTables; ++i)
            {
                var TypeNameHash = Ar.Read<ulong>();
                var NumPatches = Ar.Read<uint>();

                for (var PatchIndex = 0; PatchIndex < NumPatches; ++PatchIndex)
                {
                    var VTableOffset = Ar.Read<uint>();
                    var Offset = Ar.Read<uint>();
                }
            }

            for (var i = 0; i < NumScriptNames; ++i)
            {
                var Name = Ar.ReadFName();
                var NumPatches = Ar.Read<uint>();

                for (var PatchIndex = 0; PatchIndex < NumPatches; ++PatchIndex)
                {
                    var Offset = Ar.Read<uint>();
                }
            }

            for (var i = 0; i < NumMinimalNames; ++i)
            {
                var Name = Ar.ReadFName();
                var NumPatches = Ar.Read<uint>();

                for (var PatchIndex = 0; PatchIndex < NumPatches; ++PatchIndex)
                {
                    var Offset = Ar.Read<uint>();
                }
            }

            if (!bUseNewFormat)
            {
                ShaderMapPointerTable = new FShaderMapPointerTable(Ar);
                PointerTable = new FPointerTableBase(Ar);
            }
        }
    }

    public class FShaderMapPointerTable
    {
        public int NumTypes, NumVFTypes;

        public FShaderMapPointerTable(FArchive Ar) // LoadFromArchive
        {
            NumTypes = Ar.Read<int>();
            NumVFTypes = Ar.Read<int>();

            for (var TypeIndex = 0; TypeIndex < NumTypes; ++TypeIndex)
            {
                var TypeName = new FHashedName(Ar);
            }

            for (var VFTypeIndex = 0; VFTypeIndex < NumVFTypes; ++VFTypeIndex)
            {
                var TypeName = new FHashedName(Ar);
            }
        }
    }

    public struct FHashedName
    {
        public ulong Hash;

        public FHashedName(FArchive Ar)
        {
            Hash = Ar.Read<ulong>();
        }
    }

    public class FPointerTableBase
    {
        public int NumDependencies;

        public FPointerTableBase(FArchive Ar) // LoadFromArchive
        {
            NumDependencies = Ar.Read<int>();

            for (var i = 0; i < NumDependencies; ++i)
            {
                var NameHash = Ar.Read<ulong>();
                var SavedLayoutSize = Ar.Read<uint>();
                var SavedLayoutHash = new FSHAHash(Ar);
            }
        }
    }

    public class FMaterialShaderMap : FShaderMapBase
    {
        public FMaterialShaderMapId ShaderMapId;

        public new void Deserialize(FMaterialResourceProxyReader Ar)
        {
            ShaderMapId = new FMaterialShaderMapId(Ar);
            base.Deserialize(Ar);
        }
    }

    public class FMaterialShaderMapId
    {
        public EMaterialQualityLevel QualityLevel;
        public ERHIFeatureLevel FeatureLevel;
        public FSHAHash CookedShaderMapIdHash;
        public FPlatformTypeLayoutParameters? LayoutParams;

        public FMaterialShaderMapId(FArchive Ar)
        {
            var bIsLegacyPackage = Ar.Ver < (UE4Version) EUnrealEngineObjectUE4Version.VER_UE4_PURGED_FMATERIAL_COMPILE_OUTPUTS;

            if (!bIsLegacyPackage)
            {
                QualityLevel = (EMaterialQualityLevel) Ar.Read<int>();
                FeatureLevel = (ERHIFeatureLevel) Ar.Read<int>();
            }
            else
            {
                var LegacyQualityLevel = (EMaterialQualityLevel) Ar.Read<byte>(); // Is it enum?
            }

            CookedShaderMapIdHash = new FSHAHash(Ar);

            if (!bIsLegacyPackage)
            {
                LayoutParams = new FPlatformTypeLayoutParameters(Ar);
            }
        }
    }

    public class FPlatformTypeLayoutParameters
    {
        public uint MaxFieldAlignment;
        public EFlags Flags;

        public FPlatformTypeLayoutParameters(FArchive Ar)
        {
            MaxFieldAlignment = Ar.Read<uint>();
            Flags = Ar.Read<EFlags>();
        }

        [Flags]
        public enum EFlags
        {
            Flag_Initialized = 1 << 0,
            Flag_Is32Bit = 1 << 1,
            Flag_AlignBases = 1 << 2,
            Flag_WithEditorOnly = 1 << 3,
            Flag_WithRaytracing = 1 << 4,
        }
    }

    public enum EShaderPlatform
    {
        SP_PCD3D_SM5					= 0,
        SP_METAL						= 11,
        SP_METAL_MRT					= 12,
        SP_PCD3D_ES3_1					= 14,
        SP_OPENGL_PCES3_1				= 15,
        SP_METAL_SM5					= 16,
        SP_VULKAN_PCES3_1				= 17,
        SP_METAL_SM5_NOTESS_REMOVED		= 18,
        SP_VULKAN_SM5					= 20,
        SP_VULKAN_ES3_1_ANDROID			= 21,
        SP_METAL_MACES3_1 				= 22,
        SP_OPENGL_ES3_1_ANDROID			= 24,
        SP_METAL_MRT_MAC				= 27,
        SP_VULKAN_SM5_LUMIN_REMOVED		= 28,
        SP_VULKAN_ES3_1_LUMIN_REMOVED	= 29,
        SP_METAL_TVOS					= 30,
        SP_METAL_MRT_TVOS				= 31,
        /**********************************************************************************/
        /* !! Do not add any new platforms here. Add them below SP_StaticPlatform_Last !! */
        /**********************************************************************************/

        //---------------------------------------------------------------------------------
        /** Pre-allocated block of shader platform enum values for platform extensions */
        SP_StaticPlatform_First = 32,

        DDPI_EXTRA_SHADERPLATFORMS,

        SP_StaticPlatform_Last  = SP_StaticPlatform_First + 16 - 1,

        //  Add new platforms below this line, starting from (SP_StaticPlatform_Last + 1)
        //---------------------------------------------------------------------------------
        SP_VULKAN_SM5_ANDROID			= SP_StaticPlatform_Last+1,
        SP_PCD3D_SM6,

        SP_NumPlatforms,
        SP_NumBits						= 7,
    }
}
