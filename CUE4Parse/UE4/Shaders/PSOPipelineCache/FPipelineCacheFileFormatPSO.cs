using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.RHI;
using CUE4Parse.UE4.Readers;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Shaders
{
	public sealed class FPipelineCacheFileFormatPSO
	{
		public readonly DescriptorType Type;
		public readonly ComputeDescriptor ComputeDesc;
		public readonly GraphicsDescriptor GraphicsDesc;
		public readonly FPipelineFileCacheRayTracingDesc RayTracingDesc;
		//public readonly uint32 Hash;

		public FPipelineCacheFileFormatPSO(FArchive Ar, ref FPipelineCacheFileFormatHeader PSOheader)
		{
			Type = (DescriptorType)Ar.Read<uint>();
			switch (Type)
			{
				case DescriptorType.Compute:
					ComputeDesc = Ar.Read<ComputeDescriptor>();
					if (PSOheader.Version == (uint)EPipelineCacheFileFormatVersions.LibraryID)
						Ar.Position += 4;
					break;
				case DescriptorType.RayTracing:
					RayTracingDesc = Ar.Read<FPipelineFileCacheRayTracingDesc>();
					break;
				case DescriptorType.Graphics:
					GraphicsDesc = new GraphicsDescriptor(Ar, ref PSOheader);
					break;
				default:
					break;
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly struct ComputeDescriptor
	{
		public readonly FSHAHash ComputeShader;
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly struct FPipelineFileCacheRayTracingDesc
	{
		public readonly FSHAHash ShaderHash;
		public readonly uint MaxPayloadSizeInBytes;
		//public readonly EShaderFrequency Frequency;
		public readonly uint Frequency;
		public readonly uint bAllowHitGroupIndexing;
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

		//public readonly FVertexDeclarationElementList VertexDescriptor;
		public readonly FVertexElement[] VertexDescriptor;
		public readonly FBlendStateInitializerRHI BlendState;
		public readonly FPipelineFileCacheRasterizerState RasterizerState;
		public readonly FDepthStencilStateInitializerRHI DepthStencilState;

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
		public readonly byte? MultiViewCount;
		public readonly uint? bHasFragmentDensityAttachment;

		public GraphicsDescriptor(FArchive Ar, ref FPipelineCacheFileFormatHeader PSOheader)
		{
			VertexShader = Ar.Read<FSHAHash>();
			FragmentShader = Ar.Read<FSHAHash>();
			GeometryShader = Ar.Read<FSHAHash>();

			if (PSOheader.Version < (uint)EPipelineCacheFileFormatVersions.RemovingTessellationShaders)
			{
				HullShader = Ar.Read<FSHAHash>();
				DomainShader = Ar.Read<FSHAHash>();
			}
			if (PSOheader.Version >= (uint)EPipelineCacheFileFormatVersions.AddingMeshShaders)
			{
				MeshShader = Ar.Read<FSHAHash>();
				AmplificationShader = Ar.Read<FSHAHash>();
			}
			if (PSOheader.Version == (uint)EPipelineCacheFileFormatVersions.LibraryID)
			{
				throw new ParserException("PIPECACH version = LibraryID is not supported!");
			}

			if (PSOheader.Version < (uint)EPipelineCacheFileFormatVersions.SortedVertexDesc)
			{
				//Jesus Chraist
			}
			else
			{
				VertexDescriptor = Ar.ReadArray<FVertexElement>();
			}

			BlendState = new FBlendStateInitializerRHI(Ar);
			RasterizerState = Ar.Read<FPipelineFileCacheRasterizerState>();
			DepthStencilState = Ar.Read<FDepthStencilStateInitializerRHI>();

			RenderTargetFormats = new EPixelFormat[MaxSimultaneousRenderTargets];
			RenderTargetFlags = new ETextureCreateFlags[MaxSimultaneousRenderTargets];

			for (int i = 0; i < MaxSimultaneousRenderTargets; i++)
			{
				uint Format = Ar.Read<uint>();
				ulong RTFlags = PSOheader.Version < (uint)EPipelineCacheFileFormatVersions.MoreRenderTargetFlags 
							  ? (ulong)Ar.Read<uint>()
							  : Ar.Read<ulong>();

				RenderTargetFormats[i] = (EPixelFormat)Format;
				RenderTargetFlags[i] = ReduceRTFlags((ETextureCreateFlags)(RTFlags));

				//uint8 LoadStore = 0; // twice
				Ar.Position += 2;
			}

			RenderTargetsActive = Ar.Read<uint>();
			MSAASamples = Ar.Read<uint>();
			PrimitiveType = (EPrimitiveType)Ar.Read<uint>();
			DepthStencilFormat = (EPixelFormat)Ar.Read<uint>();

			if (PSOheader.Version < (uint)EPipelineCacheFileFormatVersions.MoreRenderTargetFlags)
				DepthStencilFlags = (ETextureCreateFlags)Ar.Read<uint>();
			else
				DepthStencilFlags = (ETextureCreateFlags)Ar.Read<ulong>();

			DepthLoad = (ERenderTargetLoadAction)Ar.Read<byte>();
			StencilLoad = (ERenderTargetLoadAction)Ar.Read<byte>();
			DepthStore = (ERenderTargetStoreAction)Ar.Read<byte>();
			StencilStore = (ERenderTargetStoreAction)Ar.Read<byte>();

			SubpassHint = Ar.Read<byte>();
			SubpassIndex = Ar.Read<byte>();

			if (PSOheader.Version >= (uint)EPipelineCacheFileFormatVersions.FragmentDensityAttachment)
			{
				MultiViewCount = Ar.Read<byte>();
				bHasFragmentDensityAttachment = Ar.Read<uint>();
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
	public readonly struct FRenderTarget
	{
		// All TEnumAsByte
		public readonly EBlendOperation ColorBlendOp;
		public readonly EBlendFactor ColorDestBlend;
		public readonly EBlendOperation AlphaBlendOp;
		public readonly EBlendFactor AlphaSrcBlend;
		public readonly EBlendFactor AlphaDestBlend;
		public readonly EColorWriteMask ColorWriteMask;
	}
	
	public readonly struct FPipelineFileCacheRasterizerState
	{
		public readonly float DepthBias;
		public readonly float SlopeScaleDepthBias;
		public readonly ERasterizerFillMode FillMode; // TEnumAsByte
		public readonly ERasterizerCullMode CullMode; // TEnumAsByte
		public readonly ERasterizerDepthClipMode? DepthClipMode; // TEnumAsByte, added in 5.1 preview
		public readonly uint bAllowMSAA;
		public readonly uint bEnableLineAA;

		public FPipelineFileCacheRasterizerState(FArchive Ar, ref FPipelineCacheFileFormatHeader PSOheader)
		{
			DepthBias = Ar.Read<float>();
			SlopeScaleDepthBias = Ar.Read<float>();
			FillMode = (ERasterizerFillMode)Ar.ReadByte();
			CullMode = (ERasterizerCullMode)Ar.ReadByte();
			if (PSOheader.Version >= (uint)EPipelineCacheFileFormatVersions.AddingDepthClipMode)
				DepthClipMode = (ERasterizerDepthClipMode)Ar.ReadByte();
			bAllowMSAA = Ar.Read<uint>();
			bEnableLineAA = Ar.Read<uint>();
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly struct FDepthStencilStateInitializerRHI
	{
		//All enums are TEnumAsByte
		public readonly uint bEnableDepthWrite;
		public readonly ECompareFunction DepthTest;
		public readonly uint bEnableFrontFaceStencil;
		public readonly ECompareFunction FrontFaceStencilTest;
		public readonly EStencilOp FrontFaceStencilFailStencilOp;
		public readonly EStencilOp FrontFaceDepthFailStencilOp;
		public readonly EStencilOp FrontFacePassStencilOp;
		public readonly uint bEnableBackFaceStencil;
		public readonly ECompareFunction BackFaceStencilTest;
		public readonly EStencilOp BackFaceStencilFailStencilOp;
		public readonly EStencilOp BackFaceDepthFailStencilOp;
		public readonly EStencilOp BackFacePassStencilOp;
		public readonly byte StencilReadMask;
		public readonly byte StencilWriteMask;
	}

	public sealed class FBlendStateInitializerRHI
	{
		public readonly FRenderTarget[] RenderTargets;
		public readonly uint bUseIndependentRenderTargetBlendStates;
		public readonly uint bUseAlphaToCoverage;

		public FBlendStateInitializerRHI(FArchive Ar)
		{
			RenderTargets = Ar.ReadArray<FRenderTarget>(GraphicsDescriptor.MaxSimultaneousRenderTargets);
			bUseIndependentRenderTargetBlendStates = Ar.Read<uint>();
			bUseAlphaToCoverage = Ar.Read<uint>();
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
		SF_NumFrequencies	= 10,
		//
		SF_NumGraphicsFrequencies = 5,
		SF_NumStandardFrequencies = 6,
		SF_NumBits			= 4,
	};

}