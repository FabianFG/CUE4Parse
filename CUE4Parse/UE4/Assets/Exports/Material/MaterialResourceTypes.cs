using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports.Niagara.NiagaraShader;
using CUE4Parse.UE4.Objects.Core.Compression;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Shaders;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public class FMaterialResource : FMaterial;

public class FMaterial
{
    public FMaterialShaderMap? LoadedShaderMap;

    public void DeserializeInlineShaderMap(FMaterialResourceProxyReader Ar)
    {
        var bCooked = Ar.Ver > EUnrealEngineObjectUE4Version.INLINE_SHADERS && Ar.ReadBoolean();
        if (!bCooked) return;

        var bValid = Ar.ReadBoolean();
        if (bValid)
        {
            LoadedShaderMap = new FMaterialShaderMap();
            LoadedShaderMap.Deserialize(Ar);

            if (Ar.Game == EGame.GAME_Stalker2) Ar.Position += 8;
        }
        else
        {
            Log.Warning("Loading a material resource '{0}' with an invalid ShaderMap!", Ar.Name);
        }
    }
}

public class FGlobalShaderCache
{
    public FGlobalShaderMap[] LoadedShaderMaps;

    public FGlobalShaderCache(FArchive Ar)
    {
        var numLoadedResources = Ar.Read<int>();
        var resourceAr = new FMaterialResourceProxyReader(Ar, false);
        LoadedShaderMaps = new FGlobalShaderMap[numLoadedResources];
        if (numLoadedResources > 0)
        {
            for (var resourceIndex = 0; resourceIndex < numLoadedResources; ++resourceIndex)
            {
                LoadedShaderMaps[resourceIndex] = new FGlobalShaderMap();
                LoadedShaderMaps[resourceIndex].Deserialize(resourceAr);
            }
        }
    }
}

public abstract class FShaderMapBase
{
    public FShaderMapContent Content;
    public FPointerTableBase PointerTable;
    public FSHAHash? ResourceHash;
    public FShaderMapResourceCode? Code;
    [JsonConverter(typeof(StringEnumConverter))] public EShaderPlatform ShaderPlatform;
    public FMemoryImageResult FrozenArchive;

    public void Deserialize(FMaterialResourceProxyReader Ar)
    {
        FrozenArchive = new FMemoryImageResult();
        FrozenArchive.LoadFromArchive(Ar, PointerTable);

        Content.Deserialize(new FMemoryImageArchive(new FByteArchive("FShaderMapContent", FrozenArchive.FrozenObject, Ar.Versions))
        {
            Names = FrozenArchive.GetNames()
        });

        var bShareCode = Ar.ReadBoolean();
        if (Ar.bUseNewFormat)
        {
            if (Ar.Game >= EGame.GAME_UE5_2)
            {
                var shaderPlatform = Ar.ReadFString();
                Enum.TryParse("SP_" + shaderPlatform, out ShaderPlatform);
            }
            else
            {
                ShaderPlatform = Ar.Read<EShaderPlatform>();
            }
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

public class FShaderMapContent
{
    public int[] ShaderHash;
    public FHashedName[] ShaderTypes;
    public int[] ShaderPermutations;
    public FShader[] Shaders;
    public FShaderPipeline[] ShaderPipelines;
    [JsonConverter(typeof(StringEnumConverter))]
    public EShaderPlatform ShaderPlatform;

    public FShaderMapContent()
    {
        ShaderHash = [];
        ShaderTypes = [];
        ShaderPermutations = [];
        Shaders = [];
        ShaderPipelines = [];
        ShaderPlatform = EShaderPlatform.SP_PCD3D_SM5;
    }

    public virtual void Deserialize(FMemoryImageArchive Ar)
    {
        ShaderHash = Ar.ReadHashTable();
        ShaderTypes = Ar.ReadArray<FHashedName>();
        ShaderPermutations = Ar.ReadArray<int>();
        Shaders = Ar.ReadArrayOfPtrs(() => new FShader(Ar));
        ShaderPipelines = Ar.ReadArrayOfPtrs(() => new FShaderPipeline(Ar));
        if (Ar.Game >= EGame.GAME_UE5_2)
        {
            var shaderPlatform = Ar.ReadFName();
            Enum.TryParse("SP_" + shaderPlatform.PlainText, out ShaderPlatform);

            if (Ar.Game is EGame.GAME_MarvelRivals or EGame.GAME_Valorant) Ar.Position += 8;
        }
        else
        {
            ShaderPlatform = Ar.Read<EShaderPlatform>();
            Ar.Position = Ar.Position.Align(8);
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

public class FShader(FMemoryImageArchive Ar)
{
    public FShaderParameterBindings Bindings = new FShaderParameterBindings(Ar);
    public FShaderParameterMapInfo ParameterMapInfo = new FShaderParameterMapInfo(Ar);
    public FHashedName[] UniformBufferParameterStructs = Ar.ReadArray<FHashedName>();
    public FShaderUniformBufferParameter[] UniformBufferParameters = Ar.ReadArray<FShaderUniformBufferParameter>();
    public ulong Type = Ar.Read<ulong>(); // TIndexedPtr<FShaderType>
    public ulong VFType = Ar.Read<ulong>(); // TIndexedPtr<FVertexFactoryType>
    public FShaderTarget Target = Ar.Read<FShaderTarget>();
    public int ResourceIndex = Ar.Read<int>();
    public uint NumInstructions = Ar.Read<uint>();
    public uint SortKey = Ar.Game >= EGame.GAME_UE5_0 ? Ar.Read<uint>() : 0;
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
        Ar.Position = Ar.Position.Align(4);
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
        [JsonConverter(typeof(StringEnumConverter))]
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
        [JsonConverter(typeof(StringEnumConverter))]
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
        Ar.Position = Ar.Position.Align(8);
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
#pragma warning disable CS0169
    private uint _packed;
#pragma warning restore CS0169
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

public class FGlobalShaderMapContent : FShaderMapContent
{
    public FHashedName HashedSourceFilename;

    public override void Deserialize(FMemoryImageArchive Ar)
    {
        base.Deserialize(Ar);
        HashedSourceFilename = Ar.Read<FHashedName>();
    }
}

public class FMaterialShaderMapContent : FShaderMapContent
{
    public FMeshMaterialShaderMap[] OrderedMeshShaderMaps;
    public FMaterialCompilationOutput MaterialCompilationOutput;
    public FSHAHash ShaderContentHash;
    public FName UserSceneTextureOutput;
    public int UserTextureDivisorX = 0;
    public int UserTextureDivisorY = 0;
    public FName ResolutionRelativeToInput;

    public override void Deserialize(FMemoryImageArchive Ar)
    {
        base.Deserialize(Ar);

        OrderedMeshShaderMaps = Ar.ReadArrayOfPtrs(() =>
        {
            var meshMaterialShaderMap = new FMeshMaterialShaderMap();
            meshMaterialShaderMap.Deserialize(Ar);

            return meshMaterialShaderMap;
        });

        MaterialCompilationOutput = new FMaterialCompilationOutput(Ar);
        ShaderContentHash = new FSHAHash(Ar);

        if (Ar.Game >= EGame.GAME_UE5_5)
        {
            UserSceneTextureOutput = Ar.ReadFName();
            UserTextureDivisorX = Ar.Read<int>();
            UserTextureDivisorY = Ar.Read<int>();
            ResolutionRelativeToInput = Ar.ReadFName();
        }
    }
}

public class FMeshMaterialShaderMap : FShaderMapContent
{
    public FHashedName VertexFactoryTypeName;

    public override void Deserialize(FMemoryImageArchive Ar)
    {
        base.Deserialize(Ar);

        VertexFactoryTypeName = Ar.Read<FHashedName>();
    }
}

public class FMaterialCompilationOutput
{
    public FUniformExpressionSet UniformExpressionSet;
    public FName[] UserSceneTextureInputs;
    public uint UsedSceneTextures;
    public byte UsedPathTracingBufferTextures;
    public FSubstrateMaterialCompilationOutput? StrataMaterialCompilationOutput;
    public byte UsedDBufferTextures;
    public byte RuntimeVirtualTextureOutputAttributeMask;

    //LAYOUT_BITFIELD(uint8, bNeedsSceneTextures, 1);
    // 5.3+ LAYOUT_BITFIELD(uint8, bUsesDBufferTextureLookup, 1);
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
    // 5.3+ LAYOUT_BITFIELD(uint8, bUsesDisplacement, 1);
    // 5.4+ LAYOUT_BITFIELD(uint8, bUsedWithNeuralNetworks, 1);
    // 5.5+ LAYOUT_BITFIELD(uint8, bUsesCustomizedUVs, 1);
    public byte b2;
    // only in 5.2
    //LAYOUT_BITFIELD(uint8, StrataMaterialType, 2);
    //LAYOUT_BITFIELD(uint8, StrataBSDFCount, 3);
    //LAYOUT_BITFIELD(uint8, StrataUintPerPixel, 8);
    public byte b3;

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public struct FSubstrateMaterialCompilationOutput
    {
        public byte SubstrateMaterialType;
        public byte SubstrateClosureCount;
        public byte SubstrateUintPerPixel;
        // only 5.3
        public byte bUsesComplexSpecialRenderPath;
    }

    public FMaterialCompilationOutput(FMemoryImageArchive Ar)
    {
        UniformExpressionSet = new FUniformExpressionSet(Ar);
        UserSceneTextureInputs = Ar.Game >= EGame.GAME_UE5_5 ? Ar.ReadArray(Ar.ReadFName) : [];
        UsedSceneTextures = Ar.Read<uint>();
        UsedPathTracingBufferTextures = Ar.Game >= EGame.GAME_UE5_3 ? Ar.Read<byte>() : (byte)0;
        if (Ar.Game >= EGame.GAME_UE5_3)
        {
            Ar.Position = Ar.Position.Align(4);
            StrataMaterialCompilationOutput = Ar.Read<FSubstrateMaterialCompilationOutput>();
        }
        UsedDBufferTextures = Ar.Read<byte>();
        RuntimeVirtualTextureOutputAttributeMask = Ar.Read<byte>();
        b1 = Ar.Read<byte>();
        b2 = Ar.Read<byte>();
        b3 = Ar.Game == EGame.GAME_UE5_2 ? Ar.Read<byte>() : (byte)0;
        Ar.Position = Ar.Position.Align(8);
    }
}

public class FUniformExpressionSet
{
    public FMaterialUniformPreshaderHeader[] UniformVectorPreshaders = [];
    public FMaterialUniformPreshaderHeader[] UniformScalarPreshaders = [];
    public FMaterialScalarParameterInfo[] UniformScalarParameters = [];
    public FMaterialVectorParameterInfo[] UniformVectorParameters = [];

    public FMaterialUniformParameterEvaluation[] UniformParameterEvaluations = [];
    public FMaterialUniformPreshaderHeader[] UniformPreshaders = [];
    public FMaterialUniformPreshaderField[]? UniformPreshaderFields;
    public FMaterialNumericParameterInfo[] UniformNumericParameters = [];
    public FMaterialTextureParameterInfo[][] UniformTextureParameters;
    public FMaterialExternalTextureParameterInfo[] UniformExternalTextureParameters;
    public FMaterialTextureCollectionParameterInfo[] UniformTextureCollectionParameters;
    public uint UniformPreshaderBufferSize;
    public FMaterialPreshaderData UniformPreshaderData;
    public byte[]? DefaultValues;
    public FMaterialVirtualTextureStack[] VTStacks;
    public FMaterialCacheTagStack[]? MaterialCacheTagStacks;
    public FGuid[] ParameterCollections;
    public FRHIUniformBufferLayoutInitializer UniformBufferLayoutInitializer;

    public FUniformExpressionSet(FMemoryImageArchive Ar)
    {
        var materialTextureParameterTypeCount = Ar.Game switch
        {
            EGame.GAME_InfinityNikki => 8,
            >= EGame.GAME_UE5_3 => 7,
            >= EGame.GAME_UE5_0 => 6,
            _ => 5,
        };

        UniformTextureParameters = new FMaterialTextureParameterInfo[materialTextureParameterTypeCount][];
        if (Ar.Game >= EGame.GAME_UE5_0)
        {
            if (Ar.Game >= EGame.GAME_UE5_6)
            {
                UniformParameterEvaluations = Ar.ReadArray<FMaterialUniformParameterEvaluation>();
            }

            if (Ar.Game is EGame.GAME_Aion2) _ = Ar.ReadArray(() => new FMaterialNumericParameterInfo(Ar)); // additional parameters
            UniformPreshaders = Ar.ReadArray(() => new FMaterialUniformPreshaderHeader(Ar));
            UniformPreshaderFields = Ar.Game >= EGame.GAME_UE5_1 ? Ar.ReadArray<FMaterialUniformPreshaderField>() : [];
            UniformNumericParameters = Ar.ReadArray(() => new FMaterialNumericParameterInfo(Ar));
            Ar.ReadArray(UniformTextureParameters, () => Ar.ReadArray(() => new FMaterialTextureParameterInfo(Ar)));
            UniformExternalTextureParameters = Ar.ReadArray(() => new FMaterialExternalTextureParameterInfo(Ar));
            if (Ar.Game >= EGame.GAME_UE5_5)
            {
                UniformTextureCollectionParameters = Ar.ReadArray(() => new FMaterialTextureCollectionParameterInfo(Ar));
            }
            UniformPreshaderBufferSize = Ar.Read<uint>();
            Ar.Position = Ar.Position.Align(8);
            UniformPreshaderData = new FMaterialPreshaderData(Ar);
            DefaultValues = Ar.ReadArray<byte>();
            using var dv = new FByteArchive("DefaultValues", DefaultValues, Ar.Versions);
            foreach (var parameter in UniformNumericParameters)
            {
                dv.Seek(parameter.DefaultValueOffset, System.IO.SeekOrigin.Begin);
                parameter.Value = parameter.ParameterType switch
                {
                    EMaterialParameterType.Scalar => dv.Read<float>(),
                    EMaterialParameterType.Vector => dv.Read<FLinearColor>(),
                    EMaterialParameterType.DoubleVector => new FVector4(dv),
                    EMaterialParameterType.StaticSwitch => dv.ReadFlag(),
                    _ => throw new NotImplementedException($"Unknown EMaterialParameterType: {parameter.ParameterType}"),
                };
            }
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
        }

        VTStacks = Ar.ReadArray(() => new FMaterialVirtualTextureStack(Ar));
        if (Ar.Game >= EGame.GAME_UE5_7)
        {
            MaterialCacheTagStacks = Ar.ReadArray<FMaterialCacheTagStack>();
        }
        ParameterCollections = Ar.ReadArray<FGuid>();
        UniformBufferLayoutInitializer = new FRHIUniformBufferLayoutInitializer(Ar);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct FMaterialCacheTagStack
{
    public FGuid TagGuid;
}

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct FMaterialUniformParameterEvaluation
{
    public ushort ParameterIndex;
    public ushort BufferOffset;
}

public class FHashedMaterialParameterInfo
{
    public FHashedName Name;
    public int Index;
    [JsonConverter(typeof(StringEnumConverter))]
    public EMaterialParameterAssociation Association;

    public FHashedMaterialParameterInfo(FMemoryImageArchive Ar)
    {
        Name = Ar.Read<FHashedName>();
        Index = Ar.Read<int>();
        Association = Ar.Read<EMaterialParameterAssociation>();
        Ar.Position = Ar.Position.Align(4);
    }
}

public class FMaterialTextureCollectionParameterInfo
{
    public FHashedMaterialParameterInfo ParameterInfo;
    public int TextureCollectionIndex;
    public bool bisVirtualCollection;

    public FMaterialTextureCollectionParameterInfo(FMemoryImageArchive Ar)
    {
        ParameterInfo = new FHashedMaterialParameterInfo(Ar);
        TextureCollectionIndex = Ar.Read<int>();

        if (Ar.Game >= EGame.GAME_UE5_7)
        {
            bisVirtualCollection = Ar.ReadBoolean();
        }
    }
}

public class FMemoryImageMaterialParameterInfo
{
    public FName Name;
    public int Index;
    [JsonConverter(typeof(StringEnumConverter))]
    public EMaterialParameterAssociation Association;

    public FMemoryImageMaterialParameterInfo(FMemoryImageArchive Ar)
    {
        Name = Ar.ReadFName();
        Index = Ar.Read<int>();
        Association = Ar.Read<EMaterialParameterAssociation>();
        Ar.Position = Ar.Position.Align(4);
    }
}

public class FMaterialBaseParameterInfo
{
    public readonly FMemoryImageMaterialParameterInfo? ParameterInfo;
    public readonly FHashedMaterialParameterInfo? ParameterInfoOld;
    public readonly string? ParameterName;

    public FMaterialBaseParameterInfo(FMemoryImageArchive Ar)
    {
        if (Ar.Game >= EGame.GAME_UE4_26)
        {
            ParameterInfo = new FMemoryImageMaterialParameterInfo(Ar);
        }
        else
        {
            ParameterInfoOld = new FHashedMaterialParameterInfo(Ar);
            ParameterName = Ar.ReadFString();
        }
    }
}

public class FMaterialScalarParameterInfo : FMaterialBaseParameterInfo
{
    public readonly float DefaultValue;

    public FMaterialScalarParameterInfo(FMemoryImageArchive Ar) : base(Ar)
    {
        DefaultValue = Ar.Read<float>();
        Ar.Position = Ar.Position.Align(8);
    }
}

public class FMaterialVectorParameterInfo : FMaterialBaseParameterInfo
{
    public readonly FLinearColor DefaultValue;

    public FMaterialVectorParameterInfo(FMemoryImageArchive Ar) : base(Ar)
    {
        DefaultValue = Ar.Read<FLinearColor>();
    }
}

public class FMaterialTextureParameterInfo : FMaterialBaseParameterInfo
{
    public int TextureIndex = -1;
    [JsonConverter(typeof(StringEnumConverter))]
    public ESamplerSourceMode SamplerSource;
    public byte VirtualTextureLayerIndex = 0;

    public FMaterialTextureParameterInfo(FMemoryImageArchive Ar) : base(Ar)
    {
        TextureIndex = Ar.Read<int>();
        SamplerSource = Ar.Read<ESamplerSourceMode>();
        VirtualTextureLayerIndex = Ar.Read<byte>();
        Ar.Position = Ar.Position.Align(4);
    }
}

public class FMaterialUniformPreshaderHeader
{
    public readonly uint OpcodeOffset;
    public readonly uint OpcodeSize;
    public readonly uint? BufferOffset;
    [JsonConverter(typeof(StringEnumConverter))]
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
            Ar.Position = Ar.Position.Align(4);
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
    [JsonConverter(typeof(StringEnumConverter))] public EShaderValueType Type;
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
    [JsonConverter(typeof(StringEnumConverter))] public EMaterialParameterType ParameterType;
    public uint DefaultValueOffset;
    public object? Value;

    public FMaterialNumericParameterInfo(FMemoryImageArchive Ar)
    {
        ParameterInfo = new FMemoryImageMaterialParameterInfo(Ar);
        ParameterType = Ar.ReadMaterialParameterType();
        Ar.Position = Ar.Position.Align(4);
        DefaultValueOffset = Ar.Read<uint>();
    }
}

public enum EMaterialParameterType : byte
{
    Scalar = 0,
    Vector,
    DoubleVector,
    Texture,
    TextureCollection,
    Font,
    RuntimeVirtualTexture,
    SparseVolumeTexture,
    StaticSwitch,
    ParameterCollection,

    NumRuntime, // Runtime parameter types must go above here, and editor-only ones below

    // TODO - Would be nice to make static parameter values editor-only, but will save that for a future-refactor
    StaticComponentMask = NumRuntime,

    Num,
    None = 0xff,
}

public enum ESamplerSourceMode : byte
{
    SSM_FromTextureAsset,
    SSM_Wrap_WorldGroupSettings,
    SSM_Clamp_WorldGroupSettings
}

public class FMaterialExternalTextureParameterInfo(FMemoryImageArchive Ar)
{
    public FName ParameterName = Ar.ReadFName();
    public FGuid ExternalTextureGuid = Ar.Read<FGuid>();
    public int SourceTextureIndex = Ar.Read<int>();
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
    public FRHIUniformBufferResource[]? GraphResources;
    public FRHIUniformBufferResource[]? GraphTextures;
    public FRHIUniformBufferResource[]? GraphBuffers;
    public FRHIUniformBufferResource[]? GraphUniformBuffers;
    public FRHIUniformBufferResource[]? UniformBuffers;
    public uint Hash = 0;
    public uint ConstantBufferSize = 0;
    public ushort RenderTargetsOffset = ushort.MaxValue;
    public byte /*FUniformBufferStaticSlot*/ StaticSlot = 255;
    [JsonConverter(typeof(StringEnumConverter))] public EUniformBufferBindingFlags BindingFlags = EUniformBufferBindingFlags.Shader;
    public bool bHasNonGraphOutputs = false;
    public bool bNoEmulatedUniformBuffer = false;
    public bool bUniformView = false;

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
            bUniformView = Ar.Game >= EGame.GAME_UE5_4 && Ar.ReadFlag();
            Ar.Position = Ar.Position.Align(4);
        }
        else if (Ar.Game >= EGame.GAME_UE4_26)
        {
            ConstantBufferSize = Ar.Read<uint>();
            StaticSlot = Ar.Read<byte>();
            Ar.Position += 1;
            RenderTargetsOffset = Ar.Read<ushort>();
            bHasNonGraphOutputs = Ar.ReadFlag();
            Ar.Position = Ar.Position.Align(8);
            Resources = Ar.ReadArray<FRHIUniformBufferResource>();
            GraphResources = Ar.ReadArray<FRHIUniformBufferResource>();
            GraphTextures = Ar.ReadArray<FRHIUniformBufferResource>();
            GraphBuffers = Ar.ReadArray<FRHIUniformBufferResource>();
            GraphUniformBuffers = Ar.ReadArray<FRHIUniformBufferResource>();
            UniformBuffers = Ar.ReadArray<FRHIUniformBufferResource>();
            uint NumUsesForDebugging = Ar.Read<uint>();
            Ar.Position = Ar.Position.Align(8);
            Name = Ar.ReadFString();
            Hash = Ar.Read<uint>();
            Ar.Position = Ar.Position.Align(8);
        }
        else//4.25
        {
            ConstantBufferSize = Ar.Read<uint>();
            StaticSlot = Ar.Read<byte>();
            Ar.Position = Ar.Position.Align(4);
            Resources = Ar.ReadArray<FRHIUniformBufferResource>();
            uint NumUsesForDebugging = Ar.Read<uint>();
            Ar.Position = Ar.Position.Align(8);
            Name = Ar.ReadFString();
            Hash = Ar.Read<uint>();
            Ar.Position = Ar.Position.Align(8);
        }
    }
}

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct FRHIUniformBufferResource
{
    public ushort MemberOffset;
    [JsonConverter(typeof(StringEnumConverter))]
    public EUniformBufferBaseType MemberType;
}

public class FShaderMapResourceCode(FArchive Ar)
{
    public FSHAHash ResourceHash = new FSHAHash(Ar);
    public FSHAHash[] ShaderHashes = Ar.ReadArray(() => new FSHAHash(Ar));
    public FShaderEntry[] ShaderEntries = Ar.Game < EGame.GAME_UE5_6 ? Ar.ReadArray(() => new FShaderEntry(Ar)) : [];
    public FShaderCodeResource[] ShaderCodeResources = Ar.Game >= EGame.GAME_UE5_6 ? Ar.ReadArray(() => new FShaderCodeResource(Ar)) : [];
}

public class FShaderEntry(FArchive Ar)
{
    public byte[] Code = Ar.ReadArray<byte>();
    public int UncompressedSize = Ar.Read<int>();
    [JsonConverter(typeof(StringEnumConverter))]
    public EShaderFrequency Frequency = Ar.Read<EShaderFrequency>();
}

public class FShaderCodeResource
{
    public struct FHeader
    {
        int UncompressedSize = 0;		// full size of code array before compression
        int ShaderCodeSize = 0;		// uncompressed size excluding optional data
        [JsonConverter(typeof(StringEnumConverter))]
        EShaderFrequency Frequency = EShaderFrequency.SF_NumFrequencies;
        byte _Pad0 = 0;
        ushort _Pad1 = 0;

        public FHeader() { }
    };

    FHeader Header;		// The above FHeader struct persisted in a shared buffer
    FSharedBuffer Code;			// The bytecode buffer as constructed by FShaderCode::FinalizeShaderCode
    FCompressedBuffer Symbols;	// Buffer containing the symbols for this bytecode; will be empty if symbols are disabled

    public FShaderCodeResource(FArchive Ar)
    {
        var headerBuffer = new FSharedBuffer(Ar);
        using var headerAr = new FByteArchive("FShaderCodeResource::Header", headerBuffer.Data, Ar.Versions);
        Header = headerAr.Read<FHeader>();
        Code = new FSharedBuffer(Ar);
        Symbols = new FCompressedBuffer(Ar);
    }
}

public class FSharedBuffer
{
    public long Len;
    public byte[] Data;

    public FSharedBuffer(FArchive Ar)
    {
        Len = Ar.Read<long>();
        Data = Ar.ReadArray<byte>((int)Len);
    }
}

public class FMemoryImageResult
{
    public FPlatformTypeLayoutParameters LayoutParameters = new FPlatformTypeLayoutParameters();
    [JsonIgnore] public byte[] FrozenObject = [];
    public FMemoryImageVTable[] VTables = [];
    public FMemoryImageName[] ScriptNames = [];
    public FMemoryImageName[] MinimalNames = [];

    public void LoadFromArchive(FMaterialResourceProxyReader Ar, FPointerTableBase pointerTable)
    {
        LayoutParameters = Ar.bUseNewFormat ? new FPlatformTypeLayoutParameters(Ar) : new();

        var frozenSize = Ar.Read<uint>();
        FrozenObject = Ar.ReadBytes((int) frozenSize);

        if (Ar.bUseNewFormat)
        {
            pointerTable.LoadFromArchive(Ar);
        }

        var numVTables = Ar.Read<int>();
        var numScriptNames = Ar.Read<int>();
        var numMinimalNames = Ar.Game >= EGame.GAME_UE4_26 ? Ar.Read<int>() : 0;
        VTables = Ar.ReadArray(numVTables, () => new FMemoryImageVTable(Ar));
        ScriptNames = Ar.ReadArray(numScriptNames, () => new FMemoryImageName(Ar));
        MinimalNames = Ar.ReadArray(numMinimalNames, () => new FMemoryImageName(Ar));

        if (!Ar.bUseNewFormat)
        {
            pointerTable.LoadFromArchive(Ar);
        }
    }

    internal Dictionary<int, (FName, bool)> GetNames()
    {
        var names = new Dictionary<int, (FName, bool)>();

        foreach (var name in ScriptNames)
        {
            foreach (var patch in name.Patches)
            {
                names[patch.Offset] = (name.Name, true);
            }
        }
        foreach (var name in MinimalNames)
        {
            foreach (var patch in name.Patches)
            {
                names[patch.Offset] = (name.Name, false);
            }
        }

        return names;
    }

    public struct FMemoryImageVTablePatch
    {
        public int VTableOffset;
        public int Offset;
    }

    public class FMemoryImageVTable(FArchive Ar)
    {
        public ulong TypeNameHash = Ar.Read<ulong>();
        public FMemoryImageVTablePatch[] Patches = Ar.ReadArray<FMemoryImageVTablePatch>();
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
        Name = Ar.ReadFString();
        Patches = Ar.ReadArray<FMemoryImageNamePatch>();
    }

    public override string ToString() => $"{Name}: x{Patches.Length} Patches";
}

public class FShaderMapPointerTable : FPointerTableBase
{
    public FHashedName[] Types;
    public FHashedName[] VFTypes;

    public override void LoadFromArchive(FMaterialResourceProxyReader Ar)
    {
        if (Ar.bUseNewFormat) base.LoadFromArchive(Ar);
        var NumTypes = Ar.Read<int>();
        var NumVFTypes = Ar.Read<int>();
        Types = Ar.ReadArray<FHashedName>(NumTypes);
        VFTypes = Ar.ReadArray<FHashedName>(NumVFTypes);
        if (!Ar.bUseNewFormat && this is not FNiagaraShaderMapPointerTable) base.LoadFromArchive(Ar);
    }
}

public struct FHashedName(FArchive Ar)
{
    public ulong Hash = Ar.Read<ulong>();
}

public class FPointerTableBase
{
    public FTypeLayoutDesc[] TypeDependencies;

    public virtual void LoadFromArchive(FMaterialResourceProxyReader Ar)
    {
        TypeDependencies = Ar.ReadArray(() => new FTypeLayoutDesc(Ar));
    }

    protected void BaseLoadFromArchive(FMaterialResourceProxyReader Ar)
    {
        TypeDependencies = Ar.ReadArray(() => new FTypeLayoutDesc(Ar));
    }
}

public class FTypeLayoutDesc
{
    public readonly object Name;
    public readonly uint SavedLayoutSize;
    public readonly FSHAHash SavedLayoutHash;

    public FTypeLayoutDesc(FMaterialResourceProxyReader Ar)
    {
        Name = Ar.bUseNewFormat ? Ar.ReadFString() : Ar.Read<FHashedName>();
        SavedLayoutSize = Ar.Read<uint>();
        SavedLayoutHash = new FSHAHash(Ar);
    }
}

public class FMaterialShaderMap : TShaderMap<FMaterialShaderMapContent, FShaderMapPointerTable>
{
    public FMaterialShaderMapId ShaderMapId;

    public FMaterialShaderMap()
    {
        ShaderMapId = new FMaterialShaderMapId();
    }

    public new void Deserialize(FMaterialResourceProxyReader Ar)
    {
        ShaderMapId = new FMaterialShaderMapId(Ar);
        base.Deserialize(Ar);
    }
}

public class FGlobalShaderMap : TShaderMap<FGlobalShaderMapContent, FShaderMapPointerTable>;

public class FMaterialShaderMapId
{
    [JsonConverter(typeof(StringEnumConverter))]
    public EMaterialQualityLevel QualityLevel;
    [JsonConverter(typeof(StringEnumConverter))]
    public ERHIFeatureLevel FeatureLevel;
    public FSHAHash? CookedShaderMapIdHash;
    public FPlatformTypeLayoutParameters? LayoutParams;

    public FMaterialShaderMapId() {}

    public FMaterialShaderMapId(FArchive Ar)
    {
        var bIsLegacyPackage = Ar.Ver < EUnrealEngineObjectUE4Version.PURGED_FMATERIAL_COMPILE_OUTPUTS;

        if (Ar.Game == EGame.GAME_OnePieceAmbition)
        {
            Ar.Position += 20;
        }

        if (!bIsLegacyPackage)
        {
            QualityLevel = Ar.Game >= EGame.GAME_UE5_2 ? (EMaterialQualityLevel) Ar.Read<byte>() : (EMaterialQualityLevel) Ar.Read<int>();//changed to byte in FN 23.20
            FeatureLevel = (ERHIFeatureLevel) Ar.Read<int>();
        }
        else
        {
            var legacyQualityLevel = (EMaterialQualityLevel) Ar.Read<byte>(); // Is it enum?
        }
        if (Ar.Game == EGame.GAME_TheFirstDescendant) Ar.Position += 4;
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
    [JsonConverter(typeof(StringEnumConverter))]
    public EFlags Flags;

    public FPlatformTypeLayoutParameters()
    {
        MaxFieldAlignment = 0xffffffff;
    }

    public FPlatformTypeLayoutParameters(FArchive Ar)
    {
        MaxFieldAlignment = Ar.Read<uint>();
        // Todo: need remap for old flag values
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

[Flags]
public enum EUniformBufferBindingFlags : byte
{
    Shader = 1 << 0,
    Static = 1 << 1,
    StaticAndShader = Static | Shader
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
