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

            if (Ar.Game == GAME_Stalker2) Ar.Position += 8;
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
    public EShaderPlatform ShaderPlatform;
    public FMemoryImageResult FrozenArchive;

    public void Deserialize(FMaterialResourceProxyReader Ar)
    {
        FrozenArchive = new FMemoryImageResult();
        FrozenArchive.LoadFromArchive(Ar, PointerTable);

        Content.Deserialize(new FMemoryImageArchive(new FByteArchive("FShaderMapContent", FrozenArchive.FrozenObject, Ar.Versions))
        {
            Names = FrozenArchive.GetNames(),
            PointerTable = PointerTable
        });

        var bShareCode = Ar.ReadBoolean();
        if (Ar.bUseNewFormat)
        {
            if (Ar.Game >= GAME_UE5_2)
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
            ResourceHash = new FSHAHash(Ar, Ar.Game >= GAME_UE5_8 ? 8 : FSHAHash.SIZE);
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
        ShaderTypes = Ar.ReadArray(() => new FHashedName(Ar));
        ShaderPermutations = Ar.ReadArray<int>();
        Shaders = Ar.ReadArrayOfPtrs(() => new FShader(Ar));
        ShaderPipelines = Ar.ReadArrayOfPtrs(() => new FShaderPipeline(Ar));
        if (Ar.Game >= GAME_UE5_2)
        {
            var shaderPlatform = Ar.ReadFName();
            Enum.TryParse("SP_" + shaderPlatform.PlainText, out ShaderPlatform);

            if (Ar.Game is GAME_MarvelRivals or GAME_Valorant or GAME_DeadByDaylight or GAME_Enginefall) Ar.Position += 8;
        }
        else
        {
            ShaderPlatform = Ar.Read<EShaderPlatform>();
            Ar.Position = Ar.Position.Align(8);
            if (Ar.Game is GAME_HonorofKingsWorld) Ar.Position += 152;
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
    public FHashedName Type; // TIndexedPtr<FShaderType>
    public FHashedName VFType; // TIndexedPtr<FVertexFactoryType>
    public FShaderTarget Target;
    public int ResourceIndex;
    public uint NumInstructions;
    public uint SortKey;
    // public FShaderCode Code; // ?

    public FShader(FMemoryImageArchive Ar)
    {
        Bindings = new FShaderParameterBindings(Ar);
        ParameterMapInfo = new FShaderParameterMapInfo(Ar);
        UniformBufferParameterStructs = Ar.ReadArray(() => new FHashedName(Ar));
        UniformBufferParameters = Ar.ReadArray<FShaderUniformBufferParameter>();

        var type = Ar.Read<ulong>() >> 1;
        Type = Ar.PointerTable is FShaderMapPointerTable pointerTable && type < (ulong)pointerTable.Types.Length ? pointerTable.Types[type] : new(type);
        var vfType = Ar.Read<ulong>() >> 1;
        VFType = Ar.PointerTable is FShaderMapPointerTable pointerTable1 && vfType < (ulong)pointerTable1.VFTypes.Length ? pointerTable1.VFTypes[vfType] : new(vfType);
        Target = Ar.Read<FShaderTarget>();
        ResourceIndex = Ar.Read<int>();
        NumInstructions = Ar.Game < GAME_UE5_6 ? Ar.Read<uint>() : 0u;
        SortKey = Ar.Game is >= GAME_UE5_0 and < GAME_UE5_9 ? Ar.Read<uint>() : 0;
    }
}

[JsonConverter(typeof(FShaderParameterBindingsJsonConverter))]
public class FShaderParameterBindings
{
    public FParameter[] Parameters;
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
        if (Ar.Game>= GAME_UE4_26)
        {
            ResourceParameters = Ar.ReadArray(() => new FResourceParameter(Ar));
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

        BindlessResourceParameters = Ar.Game >= GAME_UE5_1 ? Ar.ReadArray<FBindlessResourceParameter>() : [];
        GraphUniformBuffers = Ar.Game >= GAME_UE4_26 ? Ar.ReadArray<FParameterStructReference>() : [];
        ParameterReferences = Ar.ReadArray<FParameterStructReference>();
        if (Ar.Game is GAME_ArenaBreakoutInfinite) Ar.Position += 16;

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

    public struct FResourceParameter
    {
        public ushort ByteOffset;
        public ushort BaseIndex;
        public EUniformBufferBaseType BaseType = EUniformBufferBaseType.UBMT_INVALID;

        public FResourceParameter(FMemoryImageArchive Ar)
        {
            if (Ar.Game < GAME_UE4_26)
            {
                BaseIndex = Ar.Read<ushort>();
                ByteOffset = Ar.Read<ushort>();
            }
            else
            {
                ByteOffset = Ar.Read<ushort>();
                BaseIndex = Ar.Read<byte>();
                BaseType = Ar.Read<EUniformBufferBaseType>();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 6)]
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

public class FShaderParameterBindingsJsonConverter : JsonConverter<FShaderParameterBindings>
{
    public override void WriteJson(JsonWriter writer, FShaderParameterBindings? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        WriteArray(writer, serializer, nameof(value.Parameters), value.Parameters);
        WriteArray(writer, serializer, nameof(value.Textures), value.Textures);
        WriteArray(writer, serializer, nameof(value.SRVs), value.SRVs);
        WriteArray(writer, serializer, nameof(value.UAVs), value.UAVs);
        WriteArray(writer, serializer, nameof(value.Samplers), value.Samplers);
        WriteArray(writer, serializer, nameof(value.GraphTextures), value.GraphTextures);
        WriteArray(writer, serializer, nameof(value.GraphSRVs), value.GraphSRVs);
        WriteArray(writer, serializer, nameof(value.GraphUAVs), value.GraphUAVs);
        WriteArray(writer, serializer, nameof(value.ResourceParameters), value.ResourceParameters);
        WriteArray(writer, serializer, nameof(value.BindlessResourceParameters), value.BindlessResourceParameters);
        WriteArray(writer, serializer, nameof(value.GraphUniformBuffers), value.GraphUniformBuffers);
        WriteArray(writer, serializer, nameof(value.ParameterReferences), value.ParameterReferences);
        writer.WritePropertyName(nameof(value.StructureLayoutHash));
        writer.WriteValue(value.StructureLayoutHash);
        writer.WritePropertyName(nameof(value.RootParameterBufferIndex));
        writer.WriteValue(value.RootParameterBufferIndex);
        writer.WriteEndObject();
    }

    public override FShaderParameterBindings ReadJson(JsonReader reader, Type objectType, FShaderParameterBindings? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    private static void WriteArray<T>(JsonWriter writer, JsonSerializer serializer, string name, T[]? value)
    {
        if (value is null || value.Length == 0) return;

        writer.WritePropertyName(name);
        serializer.Serialize(writer, value);
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
        if (Ar.Game >= GAME_UE5_1)
        {
            UniformBuffers = Ar.ReadArray(() => new FShaderUniformBufferParameterInfo(Ar), false);
            TextureSamplers = Ar.ReadArray(() => new FShaderResourceParameterInfo(Ar), false);
            if (Ar.Game is GAME_DuneAwakening) Ar.Position += 16;
            SRVs = Ar.ReadArray(() => new FShaderResourceParameterInfo(Ar), false);
        }
        else //4.25-5.0
        {
            UniformBuffers = Ar.ReadArray(() => new FShaderParameterInfo(Ar), false);
            TextureSamplers = Ar.ReadArray(() => new FShaderParameterInfo(Ar), false);
            SRVs = Ar.ReadArray(() => new FShaderParameterInfo(Ar), false);
        }
        if (Ar.Game is GAME_ArenaBreakoutInfinite or GAME_HonorofKingsWorld) Ar.Position += 16;
        LooseParameterBuffers = Ar.ReadArray(() => new FShaderLooseParameterBufferInfo(Ar));
        Hash = Ar.Game >= GAME_UE4_26 ? Ar.Read<ulong>() : 0;
        if (Ar.Game is GAME_ArenaBreakoutInfinite) Ar.Position += 8;
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

[JsonConverter(typeof(FShaderResourceParameterInfoConverter))]
public class FShaderResourceParameterInfo : FShaderParameterInfo
{
    public byte BufferIndex;
    public EShaderParameterType Type;

    public FShaderResourceParameterInfo(FMemoryImageArchive Ar)
    {
        BaseIndex = Ar.Read<ushort>();
        BufferIndex = Ar.Read<byte>();
        Type = Ar.Read<EShaderParameterType>();
    }
}

public class FShaderResourceParameterInfoConverter : JsonConverter<FShaderResourceParameterInfo>
{
    public override void WriteJson(JsonWriter writer, FShaderResourceParameterInfo? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(nameof(value.BaseIndex));
        writer.WriteValue(value.BaseIndex);
        writer.WritePropertyName(nameof(value.BufferIndex));
        writer.WriteValue(value.BufferIndex);
        writer.WritePropertyName(nameof(value.Type));
        serializer.Serialize(writer, value.Type);
        writer.WriteEndObject();
    }

    public override FShaderResourceParameterInfo? ReadJson(JsonReader reader, Type objectType, FShaderResourceParameterInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum EShaderParameterType : byte
{
    LooseData,
    UniformBuffer,
    Sampler,
    SRV,
    UAV,

    BindlessSampler,
    BindlessSRV,
    BindlessUAV,

    DescriptorRange,
}

public struct FShaderUniformBufferParameter
{
    public ushort BaseIndex;
}

[JsonConverter(typeof(FShaderUniformBufferParameterInfoConverter))]
public class FShaderUniformBufferParameterInfo : FShaderParameterInfo
{
    public FShaderUniformBufferParameterInfo(FMemoryImageArchive Ar)
    {
        BaseIndex = Ar.Read<ushort>();
    }
}

public class FShaderUniformBufferParameterInfoConverter : JsonConverter<FShaderUniformBufferParameterInfo>
{
    public override void WriteJson(JsonWriter writer, FShaderUniformBufferParameterInfo? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(nameof(value.BaseIndex));
        writer.WriteValue(value.BaseIndex);
        writer.WriteEndObject();
    }

    public override FShaderUniformBufferParameterInfo? ReadJson(JsonReader reader, Type objectType, FShaderUniformBufferParameterInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public struct FShaderTarget
{
    private uint _packed;
    public EShaderFrequency Frequency => (EShaderFrequency)(_packed & 0xF);
    public EShaderPlatform Platform => (EShaderPlatform)((_packed >> 4) & 0xFFFF); // was 7 bits before 5.0

    public FShaderTarget(uint packed)
    {
        _packed = packed;
    }
}

/** The base type of a value in a shader parameter structure. */
[JsonConverter(typeof(StringEnumConverter))]
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
        HashedSourceFilename = new FHashedName(Ar);
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
        ShaderContentHash = new FSHAHash(Ar, Ar.Game >= GAME_UE5_8 ? 8 : FSHAHash.SIZE);

        if (Ar.Game >= GAME_UE5_5)
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

        VertexFactoryTypeName = new FHashedName(Ar);
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
        UserSceneTextureInputs = Ar.Game >= GAME_UE5_5 ? Ar.ReadArray(Ar.ReadFName) : [];
        UsedSceneTextures = Ar.Read<uint>();
        UsedPathTracingBufferTextures = Ar.Game >= GAME_UE5_3 ? Ar.Read<byte>() : (byte)0;
        if (Ar.Game >= GAME_UE5_3)
        {
            Ar.Position = Ar.Position.Align(4);
            StrataMaterialCompilationOutput = Ar.Read<FSubstrateMaterialCompilationOutput>();
        }
        UsedDBufferTextures = Ar.Read<byte>();
        RuntimeVirtualTextureOutputAttributeMask = Ar.Read<byte>();
        b1 = Ar.Read<byte>();
        b2 = Ar.Read<byte>();
        b3 = Ar.Game is (>= GAME_UE5_2 and < GAME_UE5_3) or >= GAME_UE5_8 ? Ar.Read<byte>() : (byte)0;
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
    public FCompactUniformExpressionSet[]? CompactUniformsVSOptional;

    public FUniformExpressionSet(FMemoryImageArchive Ar)
    {
        var materialTextureParameterTypeCount = Ar.Game switch
        {
            GAME_InfinityNikki => 8,
            >= GAME_UE5_3 => 7,
            >= GAME_UE5_0 => 6,
            _ => 5,
        };

        UniformTextureParameters = new FMaterialTextureParameterInfo[materialTextureParameterTypeCount][];
        if (Ar.Game >= GAME_UE5_0)
        {
            if (Ar.Game >= GAME_UE5_6) UniformParameterEvaluations = Ar.ReadArray<FMaterialUniformParameterEvaluation>();

            if (Ar.Game is GAME_Aion2) _ = Ar.ReadArray(() => new FMaterialNumericParameterInfo(Ar)); // additional parameters
            UniformPreshaders = Ar.ReadArray(Ar.ReadMaterialUniformPreshaderHeader);
            UniformPreshaderFields = Ar.Game is >= GAME_UE5_1 and < GAME_UE5_8 ? Ar.ReadArray<FMaterialUniformPreshaderField>() : [];
            UniformNumericParameters = Ar.ReadArray(() => new FMaterialNumericParameterInfo(Ar));
            if (Ar.Game is GAME_FateTrigger) Ar.Position += 16;
            Ar.ReadArray(UniformTextureParameters, () => Ar.ReadArray(() => new FMaterialTextureParameterInfo(Ar)));
            UniformExternalTextureParameters = Ar.ReadArray(() => new FMaterialExternalTextureParameterInfo(Ar));
            if (Ar.Game >= GAME_UE5_5 && Ar.Game is not GAME_FateTrigger) UniformTextureCollectionParameters = Ar.ReadArray(() => new FMaterialTextureCollectionParameterInfo(Ar));
            if (Ar.Game is GAME_LordOfMysteries) Ar.Position += 120;
            UniformPreshaderBufferSize = Ar.Read<uint>();
            Ar.Position = Ar.Position.Align(8);
            UniformPreshaderData = new FMaterialPreshaderData(Ar);
            DefaultValues = Ar.ReadArray<byte>();
            using var dv = new FByteArchive("DefaultValues", DefaultValues, Ar.Versions);
            foreach (var parameter in UniformNumericParameters)
            {
                dv.Seek(parameter.DefaultValueOffset, SeekOrigin.Begin);
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
            UniformVectorPreshaders = Ar.ReadArray(Ar.ReadMaterialUniformPreshaderHeader);
            UniformScalarPreshaders = Ar.ReadArray(Ar.ReadMaterialUniformPreshaderHeader);
            if (Ar.Game is GAME_TheDivisionResurgence) Ar.Position += 32;
            UniformScalarParameters = Ar.ReadArray(() => new FMaterialScalarParameterInfo(Ar));
            UniformVectorParameters = Ar.ReadArray(() => new FMaterialVectorParameterInfo(Ar));
            Ar.ReadArray(UniformTextureParameters, () => Ar.ReadArray(() => new FMaterialTextureParameterInfo(Ar)));
            UniformExternalTextureParameters = Ar.ReadArray(() => new FMaterialExternalTextureParameterInfo(Ar));
            UniformPreshaderData = new FMaterialPreshaderData(Ar);
        }

        if (Ar.Game is GAME_LordOfMysteries) Ar.Position += 16;
        VTStacks = Ar.ReadArray(() => new FMaterialVirtualTextureStack(Ar));
        if (Ar.Game >= GAME_UE5_7 || Ar.Game is GAME_FateTrigger) MaterialCacheTagStacks = Ar.ReadArray<FMaterialCacheTagStack>();
        ParameterCollections = Ar.ReadArray<FGuid>();
        if (Ar.Game is GAME_HogwartsLegacy) Ar.Position += 168;
        UniformBufferLayoutInitializer = new FRHIUniformBufferLayoutInitializer(Ar);
        if (Ar.Game >= GAME_UE5_8) CompactUniformsVSOptional = Ar.ReadArray(() => new FCompactUniformExpressionSet(Ar));
    }
}

[StructLayout(LayoutKind.Sequential, Size = 6, Pack = 2)]
public struct FCompactionMetaDataSegment
{
    public ushort SrcOffset;
    public ushort DestOffset;
    public ushort SizeToCopy;
}

public class FCompactUniformExpressionSet
{
    public FRHIUniformBufferLayoutInitializer UniformBufferLayoutInitializer;
    public FCompactionMetaDataSegment[] CompactionSegments;
    public uint UniformPreshaderBufferSize;

    public FCompactUniformExpressionSet(FMemoryImageArchive Ar)
    {
        UniformBufferLayoutInitializer = new FRHIUniformBufferLayoutInitializer(Ar);
        CompactionSegments = Ar.ReadArray<FCompactionMetaDataSegment>();
        UniformPreshaderBufferSize = Ar.Read<uint>();
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
        Name = new FHashedName(Ar);
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

        if (Ar.Game >= GAME_UE5_7)
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
        if (Ar.Game >= GAME_UE4_26)
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
    public uint OpcodeOffset;
    public uint OpcodeSize;
    public FMaterialUniformPreshaderHeader() { }

    public FMaterialUniformPreshaderHeader(FMemoryImageArchive Ar)
    {
        OpcodeOffset = Ar.Read<uint>();
        OpcodeSize = Ar.Read<uint>();
    }
}

public class FMaterialUniformPreshaderHeader_5_0 : FMaterialUniformPreshaderHeader
{
    public readonly uint BufferOffset;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EValueComponentType ComponentType;
    public readonly byte? NumComponents;

    public FMaterialUniformPreshaderHeader_5_0(FMemoryImageArchive Ar) : base(Ar)
    {
        BufferOffset = Ar.Read<uint>();
        ComponentType = Ar.Read<EValueComponentType>();
        NumComponents = Ar.Read<byte>();
        Ar.Position = Ar.Position.Align(4);
    }
}

public class FMaterialUniformPreshaderHeader_5_1 : FMaterialUniformPreshaderHeader
{
    public readonly uint FieldIndex;
    public readonly uint NumFields;

    public FMaterialUniformPreshaderHeader_5_1(FMemoryImageArchive Ar) : base(Ar)
    {
        FieldIndex = Ar.Read<uint>();
        NumFields = Ar.Read<uint>();
    }
}

public class FMaterialUniformPreshaderHeader_5_8 : FMaterialUniformPreshaderHeader
{
    [JsonConverter(typeof(StringEnumConverter))] public EShaderValueType Type;
    public readonly ushort BufferOffset;

    public FMaterialUniformPreshaderHeader_5_8(FMemoryImageArchive Ar)
    {
        Type = (EShaderValueType) Ar.Read<ushort>();
        BufferOffset = Ar.Read<ushort>();
        OpcodeOffset = Ar.Read<uint>();
        OpcodeSize = Ar.Read<uint>();
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
    public bool bPreshader2;
    public bool bPreFixup;
    public ushort Preshader2TemporarySize;

    public FMaterialPreshaderData(FMemoryImageArchive Ar)
    {
        if (Ar.Game is GAME_DuneAwakening) Ar.Position += 56; // Custom Layers Data

        if (Ar.Game >= GAME_UE5_8)
        {
            bPreshader2 = Ar.ReadFlag();
            bPreFixup = Ar.ReadFlag();
            Preshader2TemporarySize = Ar.Read<ushort>();
            Ar.Position = Ar.Position.Align(8);
        }

        if (Ar.Game >= GAME_UE4_26)
        {
            Names = Ar.ReadArray(Ar.ReadFName);
        }

        if (Ar.Game >= GAME_UE5_8)
        { }
        else if (Ar.Game >= GAME_UE5_1)
        {
            StructTypes = Ar.ReadArray<FPreshaderStructType>();
            StructComponentTypes = Ar.ReadArray<EValueComponentType>();
        }
        else if (Ar.Game >= GAME_UE5_0)
        {
            NamesOffset = Ar.ReadArray<uint>();
        }

        if (Ar.Game is GAME_HogwartsLegacy) Ar.Position += 96;

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
    public EUniformBufferBindingFlags BindingFlags = EUniformBufferBindingFlags.Shader;
    public ERHIUniformBufferFlags Flags = ERHIUniformBufferFlags.None;

    public FRHIUniformBufferLayoutInitializer(FMemoryImageArchive Ar)
    {
        if (Ar.Game is >= GAME_UE5_0 or GAME_NeedForSpeedMobile)
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
            if (Ar.Game is GAME_FateTrigger) Ar.Position += 4;
            RenderTargetsOffset = Ar.Read<ushort>();
            StaticSlot = Ar.Read<byte>();
            BindingFlags = Ar.Read<EUniformBufferBindingFlags>();
            if (Ar.Game >= GAME_UE5_5)
            {
                Flags = Ar.Read<ERHIUniformBufferFlags>();
            }
            else
            {
                if (Ar.ReadFlag()) Flags |= ERHIUniformBufferFlags.HasNonGraphOutputs;
                if (Ar.ReadFlag()) Flags |= ERHIUniformBufferFlags.NoEmulatedUniformBuffer;
                if (Ar.Game >= GAME_UE5_4 && Ar.ReadFlag()) Flags |= ERHIUniformBufferFlags.UniformView;
            }

            Ar.Position = Ar.Position.Align(4);
        }
        else if (Ar.Game >= GAME_UE4_26)
        {
            ConstantBufferSize = Ar.Read<uint>();
            StaticSlot = Ar.Read<byte>();
            Ar.Position += 1;
            RenderTargetsOffset = Ar.Read<ushort>();
            if (Ar.ReadFlag()) Flags |= ERHIUniformBufferFlags.HasNonGraphOutputs;
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
        if (Flags is not ERHIUniformBufferFlags.None) Flags &= ~ERHIUniformBufferFlags.None;
    }
}

[Flags]
[JsonConverter(typeof(StringEnumConverter))]
public enum ERHIUniformBufferFlags : byte
{
    None                    = 0,

    /** Whether to force a real uniform buffer when using emulated uniform buffers */
    NoEmulatedUniformBuffer = 1 << 0,

    /** Signals if the uniform buffer members need to be included in shader reflection */
    NeedsReflectedMembers   = 1 << 1,

    /** Whether this layout may contain non-render-graph outputs (e.g. RHI UAVs). */
    HasNonGraphOutputs      = 1 << 2,

    /** This struct is a view into uniform buffer object, on platforms that support UBO */
    UniformView             = 1 << 3,
};

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct FRHIUniformBufferResource
{
    public ushort MemberOffset;
    [JsonConverter(typeof(StringEnumConverter))]
    public EUniformBufferBaseType MemberType;
}

public class FShaderMapResourceCode(FArchive Ar)
{
    public FSHAHash ResourceHash = new FSHAHash(Ar, Ar.Game >= GAME_UE5_8 ? 8 : FSHAHash.SIZE);
    public FSHAHash[] ShaderHashes = Ar.Game >= GAME_UE5_8 ? Ar.ReadArray(() => new FSHAHash(Ar, 8)) : Ar.ReadArray(() => new FSHAHash(Ar));
    public FShaderEntry[] ShaderEntries = Ar.Game < GAME_UE5_5 ? Ar.ReadArray(() => new FShaderEntry(Ar)) : [];
    public FShaderCodeResource[] ShaderCodeResources = Ar.Game >= GAME_UE5_5 ? Ar.ReadArray(() => new FShaderCodeResource(Ar)) : [];
}

public class FShaderEntry(FArchive Ar)
{
    public byte[] Code = Ar.ReadArray<byte>();
    public int UncompressedSize = Ar.Read<int>();
    public EShaderFrequency Frequency = Ar.Read<EShaderFrequency>();
}

public class FShaderCodeResource
{
    public struct FHeader
    {
        public int UncompressedSize = 0;		// full size of code array before compression
        public int ShaderCodeSize = 0;		// uncompressed size excluding optional data
        public EShaderFrequency Frequency = EShaderFrequency.SF_NumFrequencies;
        byte _Pad0 = 0;
        ushort _Pad1 = 0;

        public FHeader() { }
    };

    public FHeader Header;		// The above FHeader struct persisted in a shared buffer
    public FSharedBuffer Code;			// The bytecode buffer as constructed by FShaderCode::FinalizeShaderCode
    public FCompressedBuffer? Symbols;	// Buffer containing the symbols for this bytecode; will be empty if symbols are disabled

    public FShaderCodeResource(FArchive Ar)
    {
        var headerBuffer = new FSharedBuffer(Ar);
        using var headerAr = new FByteArchive("FShaderCodeResource::Header", headerBuffer.Data, Ar.Versions);
        Header = headerAr.Read<FHeader>();
        Code = new FSharedBuffer(Ar);
        if (Ar.Game >= GAME_UE5_6) Symbols = new FCompressedBuffer(Ar);
    }
}

public class FSharedBuffer
{
    public long Len;
    [JsonIgnore] public byte[] Data;

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
        var numMinimalNames = Ar.Game >= GAME_UE4_26 ? Ar.Read<int>() : 0;
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
        Types = Ar.ReadArray(NumTypes, () => new FHashedName(Ar));
        VFTypes = Ar.ReadArray(NumVFTypes, () => new FHashedName(Ar));
        if (!Ar.bUseNewFormat && this is not FNiagaraShaderMapPointerTable) base.LoadFromArchive(Ar);
    }
}

[JsonConverter(typeof(FHashedNameJsonConverter))]
public struct FHashedName
{
    public ulong Hash;

    public FHashedName(ulong value)
    {
        Hash = value;
    }

    public FHashedName(FArchive Ar)
    {
        Hash = Ar.Read<ulong>();
    }

    public override string ToString()
    {
        return Hash.ToString("X16");
    }
}

public class FHashedNameJsonConverter : JsonConverter<FHashedName>
{
    public override void WriteJson(JsonWriter writer, FHashedName value, JsonSerializer serializer)
    {
        if (HashedNamesProvider.TryGetEntry(value.Hash, out var name))
        {
            writer.WriteValue(name);
            return;
        }

        writer.WriteValue(value.Hash);
    }

    public override FHashedName ReadJson(JsonReader reader, Type objectType, FHashedName existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
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
        Name = Ar.bUseNewFormat ? Ar.ReadFString() : new FHashedName(Ar);
        SavedLayoutSize = Ar.Read<uint>();
        SavedLayoutHash = new FSHAHash(Ar);

        if (Ar.bUseNewFormat) HashedNamesProvider.TryAdd((string) Name);
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

        if (Ar.Game == GAME_OnePieceAmbition)
        {
            Ar.Position += 20;
        }

        if (!bIsLegacyPackage)
        {
            QualityLevel = Ar.Game >= GAME_UE5_2 ? (EMaterialQualityLevel) Ar.Read<byte>() : (EMaterialQualityLevel) Ar.Read<int>();//changed to byte in FN 23.20
            FeatureLevel = (ERHIFeatureLevel) Ar.Read<int>();
            if (Ar.Game is GAME_ArenaBreakoutInfinite or GAME_ArenaBreakoutMobile) Ar.Position += 4;
            if (Ar.Game is GAME_RocoKingdomWorld)
            {
                (QualityLevel, FeatureLevel) = ((EMaterialQualityLevel) FeatureLevel, (ERHIFeatureLevel) QualityLevel);
                Ar.Position += 16;
            }
        }
        else if (Ar.Ver > EUnrealEngineObjectUE4Version.MATERIAL_QUALITY_LEVEL_SWITCH)
        {
            var legacyQualityLevel = (EMaterialQualityLevel) Ar.Read<byte>(); // Is it enum?
        }
        if (Ar.Game == GAME_TheFirstDescendant) Ar.Position += 4;
        CookedShaderMapIdHash = new FSHAHash(Ar, Ar.Game >= GAME_UE5_8 ? 8 : FSHAHash.SIZE);

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
[JsonConverter(typeof(StringEnumConverter))]
public enum EUniformBufferBindingFlags : byte
{
    Shader = 1 << 0,
    Static = 1 << 1,
    StaticAndShader = Static | Shader
}

[JsonConverter(typeof(StringEnumConverter))]
public enum EShaderPlatform : byte
{
    SP_PCD3D_SM5					= 0,

    SP_OPENGL_SM4                   = 1,
    SP_PS4                          = 2,
    /** Used when running in Feature Level ES2 in OpenGL. */
    SP_OPENGL_PCES2                 = 3,
    SP_XBOXONE_D3D12                = 4,
    SP_PCD3D_SM4                    = 5,
    SP_OPENGL_SM5                   = 6,
    /** Used when running in Feature Level ES2 in D3D11. */
    SP_PCD3D_ES2                    = 7,
    SP_OPENGL_ES2_ANDROID           = 8,
    SP_OPENGL_ES2_WEBGL             = 9,
    SP_OPENGL_ES2_IOS               = 10,

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
