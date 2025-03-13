using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.RHI;
using CUE4Parse.UE4.Readers;
using Org.BouncyCastle.Crypto;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Shaders;

public sealed class FPipelineCacheFileFormatPSO
{
    public readonly DescriptorType Type;
    public readonly ComputeDescriptor? ComputeDesc;
    public readonly GraphicsDescriptor? GraphicsDesc;
    public readonly FPipelineFileCacheRayTracingDesc? RayTracingDesc;
    //public readonly uint32 Hash;

    public FPipelineCacheFileFormatPSO(FArchive Ar, EPipelineCacheFileFormatVersions version)
    {
        Type = Ar.Read<DescriptorType>();
        switch (Type)
        {
            case DescriptorType.Compute:
                ComputeDesc = new ComputeDescriptor(Ar);
                if (version == EPipelineCacheFileFormatVersions.LibraryID)
                    Ar.Position += 4;
                break;
            case DescriptorType.RayTracing:
                RayTracingDesc = new FPipelineFileCacheRayTracingDesc();
                break;
            case DescriptorType.Graphics:
                GraphicsDesc = new GraphicsDescriptor(Ar, version);
                break;
            default:
                break;
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ComputeDescriptor(FArchive Ar)
{
    public readonly FSHAHash ComputeShader = new FSHAHash(Ar);
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FPipelineFileCacheRayTracingDesc(FArchive Ar)
{
    public readonly FSHAHash ShaderHash = new(Ar);
    public readonly uint MaxPayloadSizeInBytes = Ar.Read<uint>();
    //public readonly EShaderFrequency Frequency;
    public readonly uint Frequency = Ar.Read<uint>();
    public readonly uint bAllowHitGroupIndexing = Ar.Read<uint>();
}

public readonly struct GraphicsDescriptor
{
    public const int MaxSimultaneousRenderTargets = 8;

    public readonly FSHAHash VertexShader;
    public readonly FSHAHash FragmentShader;
    public readonly FSHAHash GeometryShader;
    public readonly FSHAHash? HullShader;
    public readonly FSHAHash? DomainShader;
    public readonly FSHAHash? MeshShader;
    public readonly FSHAHash? AmplificationShader;

    public readonly FVertexElement[] VertexDescriptor;
    public readonly FBlendStateInitializerRHI BlendState;
    public readonly FPipelineFileCacheRasterizerState RasterizerState;
    public readonly FDepthStencilStateInitializerRHI DepthStencilState;

    public readonly uint[]? IDs;
    public readonly EPixelFormat[] RenderTargetFormats;
    public readonly ETextureCreateFlags[] RenderTargetFlags;
    public readonly uint RenderTargetsActive;
    public readonly uint MSAASamples;

    public readonly EPixelFormat DepthStencilFormat;
    public readonly ETextureCreateFlags DepthStencilFlags;
    public readonly ERenderTargetLoadAction DepthLoad;
    public readonly ERenderTargetLoadAction StencilLoad;
    public readonly ERenderTargetStoreAction DepthStore;
    public readonly ERenderTargetStoreAction StencilStore;

    public readonly EPrimitiveType PrimitiveType;

    public readonly byte SubpassHint;
    public readonly byte SubpassIndex;
    public readonly byte MultiViewCount;
    public readonly bool bHasFragmentDensityAttachment;
    public readonly bool bDepthBounds;

    public GraphicsDescriptor(FArchive Ar, EPipelineCacheFileFormatVersions version)
    {
        VertexShader = new FSHAHash(Ar);
        FragmentShader = new FSHAHash(Ar);
        GeometryShader = new FSHAHash(Ar);

        if (version < EPipelineCacheFileFormatVersions.RemovingTessellationShaders)
        {
            HullShader = new FSHAHash(Ar);
            DomainShader = new FSHAHash(Ar);
        }
        if (version >= EPipelineCacheFileFormatVersions.AddingMeshShaders)
        {
            MeshShader = new FSHAHash(Ar);
            AmplificationShader = new FSHAHash(Ar);
        }
        if (version == EPipelineCacheFileFormatVersions.LibraryID)
        {
            IDs = Ar.ReadArray<uint>((int)EShaderFrequency.SF_Compute);
        }

        if (version < EPipelineCacheFileFormatVersions.SortedVertexDesc)
        {
            VertexDescriptor = Ar.ReadArray<FVertexElement>(); // already sorted
        }
        else
        {
            VertexDescriptor = Ar.ReadArray<FVertexElement>();
        }

        BlendState = new FBlendStateInitializerRHI(Ar);
        RasterizerState = new FPipelineFileCacheRasterizerState(Ar, version);
        DepthStencilState = new FDepthStencilStateInitializerRHI(Ar);

        RenderTargetFormats = new EPixelFormat[MaxSimultaneousRenderTargets];
        RenderTargetFlags = new ETextureCreateFlags[MaxSimultaneousRenderTargets];

        for (int i = 0; i < MaxSimultaneousRenderTargets; i++)
        {
            uint Format = Ar.Read<uint>();
            ulong RTFlags = version < EPipelineCacheFileFormatVersions.MoreRenderTargetFlags ? (ulong)Ar.Read<uint>() : Ar.Read<ulong>();

            RenderTargetFormats[i] = (EPixelFormat)Format;
            RenderTargetFlags[i] = ReduceRTFlags((ETextureCreateFlags)(RTFlags));

            //uint8 LoadStore = 0; // twice
            Ar.Position += 2;
        }

        RenderTargetsActive = Ar.Read<uint>();
        MSAASamples = Ar.Read<uint>();
        PrimitiveType = (EPrimitiveType)Ar.Read<uint>();
        DepthStencilFormat = (EPixelFormat)Ar.Read<uint>();

        if (version < EPipelineCacheFileFormatVersions.MoreRenderTargetFlags)
            DepthStencilFlags = (ETextureCreateFlags)Ar.Read<uint>();
        else
            DepthStencilFlags = (ETextureCreateFlags)Ar.Read<ulong>();

        DepthLoad = (ERenderTargetLoadAction)Ar.Read<byte>();
        StencilLoad = (ERenderTargetLoadAction)Ar.Read<byte>();
        DepthStore = (ERenderTargetStoreAction)Ar.Read<byte>();
        StencilStore = (ERenderTargetStoreAction)Ar.Read<byte>();

        SubpassHint = Ar.Read<byte>();
        SubpassIndex = Ar.Read<byte>();

        //if (version >= EPipelineCacheFileFormatVersions.FragmentDensityAttachment)
        {
            MultiViewCount = Ar.Read<byte>();
            bHasFragmentDensityAttachment = Ar.ReadBoolean();
        }

        if (version >= EPipelineCacheFileFormatVersions.AddingDepthBounds)
        {
            bDepthBounds = Ar.ReadBoolean();
        }
    }

    public static ETextureCreateFlags ReduceRTFlags(ETextureCreateFlags InFlags)
    {
        // #defines fpr (TexCreate_SRGB | TexCreate_Shared)
        return (InFlags & (ETextureCreateFlags.SRGB | ETextureCreateFlags.Shared));
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FVertexElement
{
    public readonly byte StreamIndex;
    public readonly byte Offset;
    public readonly EVertexElementType Type; // TEnumAsByte
    public readonly byte AttributeIndex;
    public readonly ushort Stride;
    public readonly ushort bUseInstanceIndex;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FRenderTarget(FArchive Ar)
{
    // All TEnumAsByte
    public readonly EBlendOperation ColorBlendOp = Ar.Read<EBlendOperation>();
    public readonly EBlendFactor ColorSrcBlend = Ar.Read<EBlendFactor>();
    public readonly EBlendFactor ColorDestBlend = Ar.Read<EBlendFactor>();
    public readonly EBlendOperation AlphaBlendOp = Ar.Read<EBlendOperation>();
    public readonly EBlendFactor AlphaSrcBlend = Ar.Read<EBlendFactor>();
    public readonly EBlendFactor AlphaDestBlend = Ar.Read<EBlendFactor>();
    public readonly EColorWriteMask ColorWriteMask = Ar.Read<EColorWriteMask>();
}

public readonly struct FPipelineFileCacheRasterizerState
{
    public readonly float DepthBias;
    public readonly float SlopeScaleDepthBias;
    public readonly ERasterizerFillMode FillMode; // TEnumAsByte
    public readonly ERasterizerCullMode CullMode; // TEnumAsByte
    public readonly ERasterizerDepthClipMode? DepthClipMode; // TEnumAsByte, added in 5.1 preview
    public readonly bool bAllowMSAA;
    public readonly bool bEnableLineAA;

    public FPipelineFileCacheRasterizerState(FArchive Ar, EPipelineCacheFileFormatVersions version)
    {
        DepthBias = Ar.Read<float>();
        SlopeScaleDepthBias = Ar.Read<float>();
        FillMode = (ERasterizerFillMode)Ar.ReadByte();
        CullMode = (ERasterizerCullMode)Ar.ReadByte();
        if (version >= EPipelineCacheFileFormatVersions.AddingDepthClipMode)
            DepthClipMode = (ERasterizerDepthClipMode)Ar.ReadByte();
        bAllowMSAA = Ar.ReadBoolean();
        if (version < EPipelineCacheFileFormatVersions.RemovingLineAA)
            bEnableLineAA = Ar.ReadBoolean();
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FDepthStencilStateInitializerRHI(FArchive Ar)
{
    //All enums are TEnumAsByte
    public readonly bool bEnableDepthWrite = Ar.ReadBoolean();
    public readonly ECompareFunction DepthTest = Ar.Read<ECompareFunction>();
    public readonly bool bEnableFrontFaceStencil = Ar.ReadBoolean();
    public readonly ECompareFunction FrontFaceStencilTest = Ar.Read<ECompareFunction>();
    public readonly EStencilOp FrontFaceStencilFailStencilOp = Ar.Read<EStencilOp>();
    public readonly EStencilOp FrontFaceDepthFailStencilOp = Ar.Read<EStencilOp>();
    public readonly EStencilOp FrontFacePassStencilOp = Ar.Read<EStencilOp>();
    public readonly bool bEnableBackFaceStencil = Ar.ReadBoolean();
    public readonly ECompareFunction BackFaceStencilTest = Ar.Read<ECompareFunction>();
    public readonly EStencilOp BackFaceStencilFailStencilOp = Ar.Read<EStencilOp>();
    public readonly EStencilOp BackFaceDepthFailStencilOp = Ar.Read<EStencilOp>();
    public readonly EStencilOp BackFacePassStencilOp = Ar.Read<EStencilOp>();
    public readonly byte StencilReadMask = Ar.Read<byte>();
    public readonly byte StencilWriteMask = Ar.Read<byte>();
}

public sealed class FBlendStateInitializerRHI
{
    public readonly FRenderTarget[] RenderTargets;
    public readonly bool bUseIndependentRenderTargetBlendStates;
    public readonly bool bUseAlphaToCoverage;

    public FBlendStateInitializerRHI(FArchive Ar)
    {
        RenderTargets = Ar.ReadArray<FRenderTarget>(GraphicsDescriptor.MaxSimultaneousRenderTargets);
        bUseIndependentRenderTargetBlendStates = Ar.ReadBoolean();
        bUseAlphaToCoverage = Ar.ReadBoolean();
    }
}

public enum DescriptorType : uint
{
    Compute = 0,
    Graphics = 1,
    RayTracing = 2,
};

public enum EShaderFrequency : byte
{
    SF_Vertex			= 0,
    SF_Mesh				= 1,
    SF_Amplification	= 2,
    SF_Pixel			= 3,
    SF_Geometry			= 4,
    SF_Compute			= 5,
    SF_RayGen			= 6,
    SF_RayMiss			= 7,
    SF_RayHitGroup		= 8,
    SF_RayCallable		= 9,
    SF_WorkGraphRoot    = 10,
    SF_WorkGraphComputeNode = 11,
    SF_NumFrequencies	= 12,
    //
    SF_NumGraphicsFrequencies = 5,
    SF_NumStandardFrequencies = 6,
    SF_NumBits			= 4,
};
