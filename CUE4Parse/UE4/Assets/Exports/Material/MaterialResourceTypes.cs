using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public class FMaterialResource : FMaterial { }

    public class FMaterial
    {
        public FMaterialShaderMap LoadedShaderMap;

        public void DeserializeInlineShaderMap(FMaterialResourceProxyReader Ar)
        {
            var bCooked = Ar.ReadBoolean();
            if (!bCooked) return;

            var bValid = Ar.ReadBoolean();
            if (bValid)
            {
                LoadedShaderMap = new FMaterialShaderMap();
                LoadedShaderMap.Deserialize(Ar);
            }
            else
            {
                Log.Warning("Loading a material resource '{0}' with an invalid ShaderMap!", Ar.Name);
            }
        }
    }

    public abstract class FShaderMapBase
    {
        public FShaderMapContent Content;
        public FSHAHash ResourceHash;
        public FShaderMapResourceCode Code;

        public void Deserialize(FMaterialResourceProxyReader Ar)
        {
            var bUseNewFormat = Ar.Versions["ShaderMap.UseNewCookedFormat"];

            var pointerTable = new FShaderMapPointerTable();
            var result = new FMemoryImageResult(pointerTable);
            result.LoadFromArchive(Ar);
            Content = ReadContent(new FMemoryImageArchive(new FByteArchive("FShaderMapContent", result.FrozenObject, Ar.Versions))
            {
                Names = result.GetNames()
            });

            var bShareCode = Ar.ReadBoolean();
            if (bUseNewFormat)
            {
                var shaderPlatform = Ar.Game >= EGame.GAME_UE5_2 ? Ar.ReadFString() : Ar.Read<EShaderPlatform>().ToString();
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

        protected abstract FShaderMapContent ReadContent(FMemoryImageArchive Ar);
    }

    public class FShaderMapContent
    {
        public int[] ShaderHash;
        public FHashedName[] ShaderTypes;
        public int[] ShaderPermutations;
        public FShader[] Shaders;
        public FShaderPipeline[] ShaderPipelines;
        public EShaderPlatform ShaderPlatform;

        public FShaderMapContent(FMemoryImageArchive Ar)
        {
            ShaderHash = Ar.ReadHashTable();
            ShaderTypes = Ar.ReadArray<FHashedName>();
            ShaderPermutations = Ar.ReadArray<int>();
            Shaders = Ar.ReadArrayOfPtrs(() => new FShader(Ar));
            ShaderPipelines = Ar.ReadArrayOfPtrs(() => new FShaderPipeline(Ar));
            if (Ar.Game >= EGame.GAME_UE5_2)
            {
                var shaderPlatform = Ar.ReadFString();
                Enum.TryParse("SP_"+shaderPlatform, out ShaderPlatform);
            }
            else
            {
                ShaderPlatform = Ar.Read<EShaderPlatform>();
                Ar.Position += 7;
            }
        }
    }

    public class FShaderPipeline
    {
        private const int SF_NumGraphicsFrequencies = 5;
        public enum EFilter
        {
            EAll,			// All pipelines
            EOnlyShared,	// Only pipelines with shared shaders
            EOnlyUnique,	// Only pipelines with unique shaders
        }

        public FHashedName TypeName;
        public FShader[] Shaders;
        public int[] PermutationIds;

        public FShaderPipeline(FMemoryImageArchive Ar)
        {
            TypeName = new FHashedName(Ar);
            Shaders = new FShader[SF_NumGraphicsFrequencies];
            for (int i = 0; i < Shaders.Length; i++)
            {
                var entryPtrPos = Ar.Position;
                var entryPtr = new FFrozenMemoryImagePtr(Ar);
                if (entryPtr.IsFrozen)
                {
                    Ar.Position = entryPtrPos + entryPtr.OffsetFromThis;
                    Shaders[i] = new FShader(Ar);
                }
                Ar.Position = (entryPtrPos + 8).Align(8);   
            }
            PermutationIds = Ar.ReadArray<int>(SF_NumGraphicsFrequencies);
        }
    }

    public class FShader
    {
        public FShaderParameterBindings Bindings;
        public FShaderParameterMapInfo ParameterMapInfo;
        public FHashedName[] UniformBufferParameterStructs;
        public FShaderUniformBufferParameter[] UniformBufferParameters;
        public ulong Type; // TIndexedPtr<FShaderType>
        public ulong VFType; // TIndexedPtr<FVertexFactoryType>
        public FShaderTarget Target;
        public int ResourceIndex;
        public uint NumInstructions;
        public uint SortKey;

        public FShader(FMemoryImageArchive Ar)
        {
            Bindings = new FShaderParameterBindings(Ar);
            ParameterMapInfo = new FShaderParameterMapInfo(Ar);
            UniformBufferParameterStructs = Ar.ReadArray<FHashedName>();
            UniformBufferParameters = Ar.ReadArray<FShaderUniformBufferParameter>();
            Type = Ar.Read<ulong>();
            VFType = Ar.Read<ulong>();
            Target = Ar.Read<FShaderTarget>();
            ResourceIndex = Ar.Read<int>();
            NumInstructions = Ar.Read<uint>();
            SortKey = Ar.Game >= EGame.GAME_UE5_0 ? Ar.Read<uint>() : 0;
        }
    }

    public class FShaderParameterBindings
    {
        public FParameter[]? Parameters;
        public FResourceParameter[]? Textures;
        public FResourceParameter[]? SRVs;
        public FResourceParameter[]? UAVs;
        public FResourceParameter[]? Samplers;
        public FResourceParameter[]? GraphTextures;
        public FResourceParameter[]? GraphSRVs;
        public FResourceParameter[]? GraphUAVs;
        public FResourceParameter[]? ResourceParameters;
        public FBindlessResourceParameter[] BindlessResourceParameters;
        public FParameterStructReference[] GraphUniformBuffers;
        public FParameterStructReference[] ParameterReferences;

        public uint StructureLayoutHash = 0;
        public ushort RootParameterBufferIndex = 0xFFFF;


        public FShaderParameterBindings(FMemoryImageArchive Ar)
        {
            Parameters = Ar.ReadArray<FParameter>();
            if (Ar.Game>= EGame.GAME_UE4_26)
            {
                ResourceParameters = Ar.ReadArray<FResourceParameter>();
            }
            else
            {
                Textures = Ar.ReadArray(() => new FResourceParameter(Ar));
                SRVs = Ar.ReadArray(() => new FResourceParameter(Ar));
                UAVs = Ar.ReadArray(() => new FResourceParameter(Ar));
                Samplers = Ar.ReadArray(() => new FResourceParameter(Ar));
                GraphTextures = Ar.ReadArray(() => new FResourceParameter(Ar));
                GraphSRVs = Ar.ReadArray(() => new FResourceParameter(Ar));
                GraphUAVs = Ar.ReadArray(() => new FResourceParameter(Ar));
            }

            BindlessResourceParameters = Ar.Game >= EGame.GAME_UE5_1 ? Ar.ReadArray<FBindlessResourceParameter>() : Array.Empty<FBindlessResourceParameter>();
            GraphUniformBuffers = Ar.Game >= EGame.GAME_UE4_26 ? Ar.ReadArray<FParameterStructReference>() : Array.Empty<FParameterStructReference>();
            ParameterReferences = Ar.ReadArray<FParameterStructReference>();

            StructureLayoutHash = Ar.Read<uint>();
            RootParameterBufferIndex = Ar.Read<ushort>();
            Ar.Position += 2;
        }

        public struct FParameter
        {
            public ushort BufferIndex;
            public ushort BaseIndex;
            public ushort ByteOffset;
            public ushort ByteSize;
        }

        [StructLayout(LayoutKind.Sequential, Size = 4)]
        public struct FResourceParameter
        {
            public ushort ByteOffset;
            public byte BaseIndex;
            public EUniformBufferBaseType BaseType = EUniformBufferBaseType.UBMT_INVALID;
            //4.26+
            //LAYOUT_FIELD(uint16, ByteOffset);
            //LAYOUT_FIELD(uint8, BaseIndex);
		    //LAYOUT_FIELD(EUniformBufferBaseType, BaseType);

            //4.26-
            //LAYOUT_FIELD(uint16, BaseIndex);
		    //LAYOUT_FIELD(uint16, ByteOffset);

            public FResourceParameter(FMemoryImageArchive Ar)
            {
                BaseIndex = (byte)Ar.Read<ushort>();
                ByteOffset = Ar.Read<ushort>();
            }

        }

        [StructLayout(LayoutKind.Sequential, Size = 8)]
        public struct FBindlessResourceParameter
        {
            public ushort ByteOffset;
            public ushort GlobalConstantOffset;
            public EUniformBufferBaseType BaseType;
        }

        [StructLayout(LayoutKind.Sequential, Size = 4)]
        public struct FParameterStructReference
        {
            public ushort BufferIndex;
            public ushort ByteOffset;
        }
    }

    public class FShaderParameterMapInfo
    {
        public FShaderParameterInfo[] UniformBuffers;
        public FShaderParameterInfo[] TextureSamplers;
        public FShaderParameterInfo[] SRVs;
        public FShaderLooseParameterBufferInfo[] LooseParameterBuffers;
        public ulong Hash;

        public FShaderParameterMapInfo(FMemoryImageArchive Ar)
        {
            if (Ar.Game >= EGame.GAME_UE5_1)
            {
                UniformBuffers = Ar.ReadArray(() => new FShaderUniformBufferParameterInfo(Ar));
                TextureSamplers = Ar.ReadArray(() => new FShaderResourceParameterInfo(Ar));
                SRVs = Ar.ReadArray(() => new FShaderResourceParameterInfo(Ar));
            }
            else //4.25-5.0
            {
                UniformBuffers = Ar.ReadArray(() => new FShaderParameterInfo(Ar));
                TextureSamplers = Ar.ReadArray(() => new FShaderParameterInfo(Ar));
                SRVs = Ar.ReadArray(() => new FShaderParameterInfo(Ar));
            }
            LooseParameterBuffers = Ar.ReadArray(() => new FShaderLooseParameterBufferInfo(Ar));
            Hash = Ar.Game >= EGame.GAME_UE4_26 ? Ar.Read<ulong>() : 0;
        }
    }

    public class FShaderLooseParameterBufferInfo
    {
        public ushort BaseIndex, Size;
        public FShaderLooseParameterInfo[] Parameters;

        public FShaderLooseParameterBufferInfo(FMemoryImageArchive Ar)
        {
            BaseIndex = Ar.Read<ushort>();
            Size = Ar.Read<ushort>();
            Ar.Position += 4;
            Parameters = Ar.ReadArray<FShaderLooseParameterInfo>();
        }
    }

    public class FShaderParameterInfo
    {
        public ushort BaseIndex;
        public ushort Size;

        public FShaderParameterInfo(FMemoryImageArchive Ar)
        {
            BaseIndex = Ar.Read<ushort>();
            Size = Ar.Read<ushort>();
        }

        public FShaderParameterInfo() { }
    }
    public struct FShaderLooseParameterInfo
    {
        public ushort BaseIndex, Size;
    }

    public class FShaderResourceParameterInfo : FShaderParameterInfo
    {
        public byte BufferIndex;
        public byte Type; // EShaderParameterType

        public FShaderResourceParameterInfo(FMemoryImageArchive Ar)
        {
            BaseIndex = Ar.Read<ushort>();
            BufferIndex = Ar.Read<byte>();
            Type = Ar.Read<byte>();   
        }
    }

    public struct FShaderUniformBufferParameter
    {
        public ushort BaseIndex;
    }

    public class FShaderUniformBufferParameterInfo : FShaderParameterInfo
    {
        public FShaderUniformBufferParameterInfo(FMemoryImageArchive Ar)
        {
            BaseIndex = Ar.Read<ushort>();
        }
    }

    public struct FShaderTarget
    {
        private uint _packed;
    }

    /** The base type of a value in a shader parameter structure. */
    public enum EUniformBufferBaseType : byte
    {
        UBMT_INVALID,

        // Invalid type when trying to use bool, to have explicit error message to programmer on why
        // they shouldn't use bool in shader parameter structures.
        UBMT_BOOL,

        // Parameter types.
        UBMT_INT32,
        UBMT_UINT32,
        UBMT_FLOAT32,

        // RHI resources not tracked by render graph.
        UBMT_TEXTURE,
        UBMT_SRV,
        UBMT_UAV,
        UBMT_SAMPLER,

        // Resources tracked by render graph.
        UBMT_RDG_TEXTURE,
        UBMT_RDG_TEXTURE_ACCESS,
        UBMT_RDG_TEXTURE_ACCESS_ARRAY,
        UBMT_RDG_TEXTURE_SRV,
        UBMT_RDG_TEXTURE_UAV,
        UBMT_RDG_BUFFER_ACCESS,
        UBMT_RDG_BUFFER_ACCESS_ARRAY,
        UBMT_RDG_BUFFER_SRV,
        UBMT_RDG_BUFFER_UAV,
        UBMT_RDG_UNIFORM_BUFFER,

        // Nested structure.
        UBMT_NESTED_STRUCT,

        // Structure that is nested on C++ side, but included on shader side.
        UBMT_INCLUDED_STRUCT,

        // GPU Indirection reference of struct, like is currently named Uniform buffer.
        UBMT_REFERENCED_STRUCT,

        // Structure dedicated to setup render targets for a rasterizer pass.
        UBMT_RENDER_TARGET_BINDING_SLOTS,

        EUniformBufferBaseType_Num,
        EUniformBufferBaseType_NumBits = 5,
    }

    public class FMaterialShaderMapContent : FShaderMapContent
    {
        public FMeshMaterialShaderMap[] OrderedMeshShaderMaps;
        public FMaterialCompilationOutput MaterialCompilationOutput;
        public FSHAHash ShaderContentHash;

        public FMaterialShaderMapContent(FMemoryImageArchive Ar) : base(Ar)
        {
            OrderedMeshShaderMaps = Ar.ReadArrayOfPtrs(() => new FMeshMaterialShaderMap(Ar));
            MaterialCompilationOutput = new FMaterialCompilationOutput(Ar);
            ShaderContentHash = new FSHAHash(Ar);
        }
    }

    public class FMeshMaterialShaderMap : FShaderMapContent
    {
        public FHashedName VertexFactoryTypeName;

        public FMeshMaterialShaderMap(FMemoryImageArchive Ar) : base(Ar)
        {
            VertexFactoryTypeName = Ar.Read<FHashedName>();
        }
    }

    public class FMaterialCompilationOutput
    {
        public FUniformExpressionSet UniformExpressionSet;
        public uint UsedSceneTextures;
        public byte UsedDBufferTextures;
        public byte RuntimeVirtualTextureOutputAttributeMask;

        //LAYOUT_BITFIELD(uint8, bNeedsSceneTextures, 1);
        //LAYOUT_BITFIELD(uint8, bUsesEyeAdaptation, 1);
        //LAYOUT_BITFIELD(uint8, bModifiesMeshPosition, 1);
        //LAYOUT_BITFIELD(uint8, bUsesWorldPositionOffset, 1);
        //LAYOUT_BITFIELD(uint8, bUsesGlobalDistanceField, 1);
        //LAYOUT_BITFIELD(uint8, bUsesPixelDepthOffset, 1);
        //LAYOUT_BITFIELD(uint8, bUsesDistanceCullFade, 1);
        //LAYOUT_BITFIELD(uint8, bUsesPerInstanceCustomData, 1);
        public byte b1;

        //LAYOUT_BITFIELD(uint8, bUsesPerInstanceRandom, 1);
        //LAYOUT_BITFIELD(uint8, bUsesVertexInterpolator, 1);
        //LAYOUT_BITFIELD(uint8, bHasRuntimeVirtualTextureOutputNode, 1);
        //LAYOUT_BITFIELD(uint8, bUsesAnisotropy, 1);
        public byte b2;

        public FMaterialCompilationOutput(FMemoryImageArchive Ar)
        {
            UniformExpressionSet = new FUniformExpressionSet(Ar);
            UsedSceneTextures = Ar.Read<uint>();
            UsedDBufferTextures = Ar.Read<byte>();
            RuntimeVirtualTextureOutputAttributeMask = Ar.Read<byte>();
            b1 = Ar.Read<byte>();
            b2 = Ar.Read<byte>();
        }
    }

    public class FUniformExpressionSet
    {
        public FMaterialUniformPreshaderHeader[] UniformVectorPreshaders = Array.Empty<FMaterialUniformPreshaderHeader>();
        public FMaterialUniformPreshaderHeader[] UniformScalarPreshaders = Array.Empty<FMaterialUniformPreshaderHeader>();
        public FMaterialScalarParameterInfo[] UniformScalarParameters = Array.Empty<FMaterialScalarParameterInfo>();
        public FMaterialVectorParameterInfo[] UniformVectorParameters = Array.Empty<FMaterialVectorParameterInfo>();

        public FMaterialUniformPreshaderHeader[] UniformPreshaders = Array.Empty<FMaterialUniformPreshaderHeader>();
        public FMaterialUniformPreshaderField[]? UniformPreshaderFields;
        public FMaterialNumericParameterInfo[] UniformNumericParameters = Array.Empty<FMaterialNumericParameterInfo>();
        public readonly FMaterialTextureParameterInfo[][] UniformTextureParameters = new FMaterialTextureParameterInfo[6][];
        public FMaterialExternalTextureParameterInfo[] UniformExternalTextureParameters;
        public uint UniformPreshaderBufferSize;
        public FMaterialPreshaderData UniformPreshaderData;
        public byte[] DefaultValues;
        public FMaterialVirtualTextureStack[] VTStacks;
        public FGuid[] ParameterCollections;
        public FRHIUniformBufferLayoutInitializer UniformBufferLayoutInitializer;

        public FUniformExpressionSet(FMemoryImageArchive Ar)
        {
            if (Ar.Game >= EGame.GAME_UE5_0)
            {
                UniformPreshaders = Ar.ReadArray(() => new FMaterialUniformPreshaderHeader(Ar));
                UniformPreshaderFields = Ar.Game >= EGame.GAME_UE5_1 ? Ar.ReadArray<FMaterialUniformPreshaderField>() : Array.Empty<FMaterialUniformPreshaderField>();
                UniformNumericParameters = Ar.ReadArray(() => new FMaterialNumericParameterInfo(Ar));
                Ar.ReadArray(UniformTextureParameters, () => Ar.ReadArray(() => new FMaterialTextureParameterInfo(Ar)));
                UniformExternalTextureParameters = Ar.ReadArray(() => new FMaterialExternalTextureParameterInfo(Ar));
                UniformPreshaderBufferSize = Ar.Read<uint>();
                Ar.Position += 4;
                UniformPreshaderData = new FMaterialPreshaderData(Ar);
                DefaultValues = Ar.ReadArray<byte>();
                VTStacks = Ar.ReadArray(() => new FMaterialVirtualTextureStack(Ar));
                ParameterCollections = Ar.ReadArray<FGuid>();
                UniformBufferLayoutInitializer = new FRHIUniformBufferLayoutInitializer(Ar);
            }
            else
            {
                UniformVectorPreshaders = Ar.ReadArray(() => new FMaterialUniformPreshaderHeader(Ar));
                UniformScalarPreshaders = Ar.ReadArray(() => new FMaterialUniformPreshaderHeader(Ar));
                UniformScalarParameters = Ar.ReadArray(() => new FMaterialScalarParameterInfo(Ar));
                UniformVectorParameters = Ar.ReadArray(() => new FMaterialVectorParameterInfo(Ar));
                UniformTextureParameters = new FMaterialTextureParameterInfo[5][];
                Ar.ReadArray(UniformTextureParameters, () => Ar.ReadArray(() => new FMaterialTextureParameterInfo(Ar)));
                UniformExternalTextureParameters = Ar.ReadArray(() => new FMaterialExternalTextureParameterInfo(Ar));
                UniformPreshaderData = new FMaterialPreshaderData(Ar);
                VTStacks = Ar.ReadArray(() => new FMaterialVirtualTextureStack(Ar));
                ParameterCollections = Ar.ReadArray<FGuid>();
                UniformBufferLayoutInitializer = new FRHIUniformBufferLayoutInitializer(Ar);
            }
        }
    }

    public class FMaterialScalarParameterInfo
    {
        public readonly FMemoryImageMaterialParameterInfo ParameterInfo;
        public readonly float DefaultValue;

        public FMaterialScalarParameterInfo(FMemoryImageArchive Ar)
        {
            ParameterInfo = new FMemoryImageMaterialParameterInfo(Ar);
            DefaultValue = Ar.Read<float>();
            Ar.Position +=4;
        }
    }

    public class FMaterialVectorParameterInfo
    {
        public readonly FMemoryImageMaterialParameterInfo ParameterInfo;
        public readonly FLinearColor DefaultValue;

        public FMaterialVectorParameterInfo(FMemoryImageArchive Ar)
        {
            ParameterInfo = new FMemoryImageMaterialParameterInfo(Ar);
            DefaultValue = Ar.Read<FLinearColor>();
        }
    }

    public class FMaterialUniformPreshaderHeader
    {
        public readonly uint OpcodeOffset;
        public readonly uint OpcodeSize;
        public readonly uint? BufferOffset;
        public readonly EValueComponentType? ComponentType;
        public readonly byte? NumComponents;
        public readonly uint? FieldIndex;
        public readonly uint? NumFields;

        public FMaterialUniformPreshaderHeader(FMemoryImageArchive Ar)
        {
            OpcodeOffset = Ar.Read<uint>();
            OpcodeSize = Ar.Read<uint>();

            if (Ar.Game == EGame.GAME_UE5_0)
            {
                BufferOffset = Ar.Read<uint>();
                ComponentType = Ar.Read<EValueComponentType>();
                NumComponents = Ar.Read<byte>();
                Ar.Position += 2;
            }
            else if (Ar.Game >= EGame.GAME_UE5_1)
            {
                FieldIndex = Ar.Read<uint>();
                NumFields = Ar.Read<uint>();
            }
        }
    }


    [StructLayout(LayoutKind.Sequential, Size = 12)]
    public struct FMaterialUniformPreshaderField
    {
        public uint BufferOffset, ComponentIndex;
        public EShaderValueType Type;
    }

    public enum EShaderValueType : byte
    {
        Void,

        Float1,
        Float2,
        Float3,
        Float4,

        Double1,
        Double2,
        Double3,
        Double4,

        Int1,
        Int2,
        Int3,
        Int4,

        Bool1,
        Bool2,
        Bool3,
        Bool4,

        // Any scalar/vector type
        Numeric1,
        Numeric2,
        Numeric3,
        Numeric4,

        // float4x4
        Float4x4,

        // Both of these are double4x4 on CPU
        // On GPU, they map to FLWCMatrix and FLWCInverseMatrix
        Double4x4,
        DoubleInverse4x4,

        // Any matrix type
        Numeric4x4,

        Struct,
        Object,
        Any,

        Num,
    }

    public class FMaterialNumericParameterInfo
    {
        public FMemoryImageMaterialParameterInfo ParameterInfo;
        public EMaterialParameterType ParameterType;
        public uint DefaultValueOffset;

        public FMaterialNumericParameterInfo(FMemoryImageArchive Ar)
        {
            ParameterInfo = new FMemoryImageMaterialParameterInfo(Ar);
            ParameterType = Ar.Read<EMaterialParameterType>();
            Ar.Position += 3;
            DefaultValueOffset = Ar.Read<uint>();
        }
    }

    public enum EMaterialParameterType : byte
    {
        Scalar = 0,
        Vector,
        DoubleVector,
        Texture,
        Font,
        RuntimeVirtualTexture,

        NumRuntime, // Runtime parameter types must go above here, and editor-only ones below

        StaticSwitch = NumRuntime,
        StaticComponentMask,

        Num,
        None = 0xff,
    }

    public class FMaterialTextureParameterInfo
    {
        public FMemoryImageMaterialParameterInfo ParameterInfo;
        public int TextureIndex = -1;
        public ESamplerSourceMode SamplerSource;
        public byte VirtualTextureLayerIndex = 0;

        public FMaterialTextureParameterInfo(FMemoryImageArchive Ar)
        {
            ParameterInfo = new FMemoryImageMaterialParameterInfo(Ar);
            TextureIndex = Ar.Read<int>();
            SamplerSource = Ar.Read<ESamplerSourceMode>();
            VirtualTextureLayerIndex = Ar.Read<byte>();
            Ar.Position += 2;
        }
    }

    public class FMemoryImageMaterialParameterInfo
    {
        public FName Name;
        public int Index;
        public EMaterialParameterAssociation Association;

        public FMemoryImageMaterialParameterInfo(FMemoryImageArchive Ar)
        {
            Name = Ar.ReadFName();
            Index = Ar.Read<int>();
            Association = Ar.Read<EMaterialParameterAssociation>();
            Ar.Position += 3;
        }
    }

    public enum ESamplerSourceMode : byte
    {
        SSM_FromTextureAsset,
        SSM_Wrap_WorldGroupSettings,
        SSM_Clamp_WorldGroupSettings
    }

    public class FMaterialExternalTextureParameterInfo
    {
        public FName ParameterName;
        public FGuid ExternalTextureGuid;
        public int SourceTextureIndex;

        public FMaterialExternalTextureParameterInfo(FMemoryImageArchive Ar)
        {
            ParameterName = Ar.ReadFName();
            ExternalTextureGuid = Ar.Read<FGuid>();
            SourceTextureIndex = Ar.Read<int>();
        }
    }

    public class FMaterialPreshaderData
    {
        public FName[]? Names;
        public uint[]? NamesOffset;
        public FPreshaderStructType[]? StructTypes;
        public EValueComponentType[]? StructComponentTypes;
        public byte[] Data;

        public FMaterialPreshaderData(FMemoryImageArchive Ar)
        {
            if (Ar.Game >= EGame.GAME_UE4_26)
            {
                Names = Ar.ReadArray(Ar.ReadFName);
            }

            if (Ar.Game == EGame.GAME_UE5_0)
            {
                NamesOffset = Ar.ReadArray<uint>();
            }
            else if (Ar.Game >= EGame.GAME_UE5_1)
            {
                StructTypes = Ar.ReadArray<FPreshaderStructType>();
                StructComponentTypes = Ar.ReadArray<EValueComponentType>();
            }

            Data = Ar.ReadArray<byte>();
        }
    }

    public struct FPreshaderStructType
    {
        public ulong Hash;
        public int ComponentTypeIndex;
        public int NumComponents;
    }

    public enum EValueComponentType : byte
    {
        Void,
        Float,
        Double,
        Int,
        Bool,

        // May be any numeric type, stored internally as 'double' within FValue
        Numeric,

        Num,
    }

    public class FMaterialVirtualTextureStack
    {
        public uint NumLayers;
        public readonly int[] LayerUniformExpressionIndices = new int[8];
        public int PreallocatedStackTextureIndex;

        public FMaterialVirtualTextureStack(FMemoryImageArchive Ar)
        {
            NumLayers = Ar.Read<uint>();
            Ar.ReadArray(LayerUniformExpressionIndices);
            PreallocatedStackTextureIndex = Ar.Read<int>();
        }
    }

    public class FRHIUniformBufferLayoutInitializer
    {
        public string Name;
        public FRHIUniformBufferResource[] Resources;
        public FRHIUniformBufferResource[] GraphResources;
        public FRHIUniformBufferResource[] GraphTextures;
        public FRHIUniformBufferResource[] GraphBuffers;
        public FRHIUniformBufferResource[] GraphUniformBuffers;
        public FRHIUniformBufferResource[] UniformBuffers;
        public uint Hash = 0;
        public uint ConstantBufferSize = 0;
        public ushort RenderTargetsOffset = ushort.MaxValue;
        public byte /*FUniformBufferStaticSlot*/
            StaticSlot = 255;
        public EUniformBufferBindingFlags BindingFlags = EUniformBufferBindingFlags.Shader;
        public bool bHasNonGraphOutputs = false;
        public bool bNoEmulatedUniformBuffer = false;

        public FRHIUniformBufferLayoutInitializer(FMemoryImageArchive Ar)
        {
            if (Ar.Game >= EGame.GAME_UE5_0)
            {
                Name = Ar.ReadFString();
                Resources = Ar.ReadArray<FRHIUniformBufferResource>();
                GraphResources = Ar.ReadArray<FRHIUniformBufferResource>();
                GraphTextures = Ar.ReadArray<FRHIUniformBufferResource>();
                GraphBuffers = Ar.ReadArray<FRHIUniformBufferResource>();
                GraphUniformBuffers = Ar.ReadArray<FRHIUniformBufferResource>();
                UniformBuffers = Ar.ReadArray<FRHIUniformBufferResource>();
                Hash = Ar.Read<uint>();
                ConstantBufferSize = Ar.Read<uint>();
                RenderTargetsOffset = Ar.Read<ushort>();
                StaticSlot = Ar.Read<byte>();
                BindingFlags = Ar.Read<EUniformBufferBindingFlags>();
                bHasNonGraphOutputs = Ar.ReadFlag();
                bNoEmulatedUniformBuffer = Ar.ReadFlag();
                Ar.Position += 2;
            }
            else if (Ar.Game >= EGame.GAME_UE4_26)
            {
                ConstantBufferSize = Ar.Read<uint>();
                StaticSlot = Ar.Read<byte>();
                Ar.Position +=1;
                RenderTargetsOffset = Ar.Read<ushort>();
                bHasNonGraphOutputs = Ar.ReadFlag();
                Ar.Position +=7;
                Resources = Ar.ReadArray<FRHIUniformBufferResource>();
                GraphResources = Ar.ReadArray<FRHIUniformBufferResource>();
                GraphTextures = Ar.ReadArray<FRHIUniformBufferResource>();
                GraphBuffers = Ar.ReadArray<FRHIUniformBufferResource>();
                GraphUniformBuffers = Ar.ReadArray<FRHIUniformBufferResource>();
                UniformBuffers = Ar.ReadArray<FRHIUniformBufferResource>();
                uint NumUsesForDebugging = Ar.Read<uint>();
                Ar.Position += 4;
                Name = Ar.ReadFString();
                Hash = Ar.Read<uint>();
                Ar.Position += 4;
            }
            else//4.25
            {
                ConstantBufferSize = Ar.Read<uint>();
                StaticSlot = Ar.Read<byte>();
                Ar.Position += 3;
                Resources = Ar.ReadArray<FRHIUniformBufferResource>();
                uint NumUsesForDebugging = Ar.Read<uint>();
                Ar.Position += 4;
                Name = Ar.ReadFString();
                Hash = Ar.Read<uint>();
                Ar.Position += 4;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public struct FRHIUniformBufferResource
    {
        public ushort MemberOffset;
        public EUniformBufferBaseType MemberType;
    }

    public enum EUniformBufferBindingFlags : byte
    {
        Shader = 1 << 0,
        Static = 1 << 1,
        StaticAndShader = Static | Shader
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
        public FPlatformTypeLayoutParameters LayoutParameters;
        public byte[] FrozenObject;
        public FPointerTableBase PointerTable;
        public FMemoryImageVTable[] VTables;
        public FMemoryImageName[] ScriptNames;
        public FMemoryImageName[] MinimalNames;

        public FMemoryImageResult(FPointerTableBase pointerTable)
        {
            PointerTable = pointerTable;
        }

        public void LoadFromArchive(FArchive Ar)
        {
            var bUseNewFormat = Ar.Versions["ShaderMap.UseNewCookedFormat"];

            LayoutParameters = bUseNewFormat ? new FPlatformTypeLayoutParameters(Ar) : new();

            var frozenSize = Ar.Read<uint>();
            FrozenObject = Ar.ReadBytes((int) frozenSize);

            if (bUseNewFormat)
            {
                PointerTable.LoadFromArchive(Ar, true);
            }

            var numVTables = Ar.Read<int>();
            var numScriptNames = Ar.Read<int>();
            var numMinimalNames = Ar.Game >= EGame.GAME_UE4_26 ? Ar.Read<int>() : 0;
            VTables = Ar.ReadArray(numVTables, () => new FMemoryImageVTable(Ar));
            ScriptNames = Ar.ReadArray(numScriptNames, () => new FMemoryImageName(Ar));
            MinimalNames = Ar.ReadArray(numMinimalNames, () => new FMemoryImageName(Ar));

            if (!bUseNewFormat)
            {
                PointerTable.LoadFromArchive(Ar, false);
            }
        }

        internal Dictionary<int, FName> GetNames()
        {
            var names = new Dictionary<int, FName>();

            foreach (var name in ScriptNames)
            {
                foreach (var patch in name.Patches)
                {
                    names[patch.Offset] = name.Name;
                }
            }
            foreach (var name in MinimalNames)
            {
                foreach (var patch in name.Patches)
                {
                    names[patch.Offset] = name.Name;
                }
            }

            return names;
        }

        public struct FMemoryImageVTablePatch
        {
            public int VTableOffset;
            public int Offset;
        }

        public class FMemoryImageVTable
        {
            public ulong TypeNameHash;
            public FMemoryImageVTablePatch[] Patches;

            public FMemoryImageVTable(FArchive Ar)
            {
                TypeNameHash = Ar.Read<ulong>();
                Patches = Ar.ReadArray<FMemoryImageVTablePatch>();
            }
        }

        public struct FMemoryImageNamePatch
        {
            public int Offset;
        }

        public class FMemoryImageName
        {
            public FName Name;
            public FMemoryImageNamePatch[] Patches;

            public FMemoryImageName(FArchive Ar)
            {
                Name = Ar.ReadFName();
                Patches = Ar.ReadArray<FMemoryImageNamePatch>();
            }

            public override string ToString() => $"{Name}: x{Patches.Length} Patches";
        }
    }

    public class FShaderMapPointerTable : FPointerTableBase
    {
        //public int NumTypes, NumVFTypes;
        public FHashedName[]? Types;
        public FHashedName[]? VFTypes;

        public override void LoadFromArchive(FArchive Ar, bool bUseNewFormat)
        {
            if (bUseNewFormat) base.LoadFromArchive(Ar, bUseNewFormat);
            var NumTypes = Ar.Read<int>();
            var NumVFTypes = Ar.Read<int>();
            Types = Ar.ReadArray<FHashedName>(NumTypes);
            VFTypes = Ar.ReadArray<FHashedName>(NumVFTypes);
            if (!bUseNewFormat) base.LoadFromArchive(Ar, bUseNewFormat);
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
        public FTypeLayoutDesc[] TypeDependencies;

        public virtual void LoadFromArchive(FArchive Ar, bool bUseNewFormat)
        {
            TypeDependencies = Ar.ReadArray(() => new FTypeLayoutDesc(Ar, bUseNewFormat));
        }
    }

    public class FTypeLayoutDesc
    {   
        public readonly FName? Name;
        public readonly FHashedName? NameHash;
        public readonly uint SavedLayoutSize;
        public readonly FSHAHash SavedLayoutHash;

        public FTypeLayoutDesc(FArchive Ar, bool bUseNewFormat)
        {
            if (bUseNewFormat)
            {
                Name = Ar.ReadFName();
            }
            else
            {
                NameHash = Ar.Read<FHashedName>();
            }
            SavedLayoutSize = Ar.Read<uint>();
            SavedLayoutHash = new FSHAHash(Ar);
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

        protected override FShaderMapContent ReadContent(FMemoryImageArchive Ar) => new FMaterialShaderMapContent(Ar);
    }

    public class FMaterialShaderMapId
    {
        public EMaterialQualityLevel QualityLevel;
        public ERHIFeatureLevel FeatureLevel;
        public FSHAHash CookedShaderMapIdHash;
        public FPlatformTypeLayoutParameters? LayoutParams;

        public FMaterialShaderMapId(FArchive Ar)
        {
            var bIsLegacyPackage = Ar.Ver < EUnrealEngineObjectUE4Version.PURGED_FMATERIAL_COMPILE_OUTPUTS;

            if (!bIsLegacyPackage)
            {
                QualityLevel = Ar.Game >= EGame.GAME_UE5_2 ? (EMaterialQualityLevel) Ar.Read<byte>() : (EMaterialQualityLevel) Ar.Read<int>();//changed to byte in FN 23.20 
                FeatureLevel = (ERHIFeatureLevel) Ar.Read<int>();
            }
            else
            {
                var legacyQualityLevel = (EMaterialQualityLevel) Ar.Read<byte>(); // Is it enum?
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

        public FPlatformTypeLayoutParameters()
        {
            MaxFieldAlignment = 0xffffffff;
        }

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

    public enum EShaderPlatform : byte
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
