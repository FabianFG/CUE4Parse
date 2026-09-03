using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public enum EMaterialSamplerType
    {
        SAMPLERTYPE_Color,
        SAMPLERTYPE_Grayscale,
        SAMPLERTYPE_Alpha,
        SAMPLERTYPE_Normal,
        SAMPLERTYPE_Masks,
        SAMPLERTYPE_DistanceFieldFont,
        SAMPLERTYPE_LinearColor,
        SAMPLERTYPE_LinearGrayscale,
        SAMPLERTYPE_Data,
        SAMPLERTYPE_External,

        SAMPLERTYPE_VirtualColor,
        SAMPLERTYPE_VirtualGrayscale,
        SAMPLERTYPE_VirtualAlpha,
        SAMPLERTYPE_VirtualNormal,
        SAMPLERTYPE_VirtualMasks,
        /*No DistanceFiledFont Virtual*/
        SAMPLERTYPE_VirtualLinearColor,
        SAMPLERTYPE_VirtualLinearGrayscale,
        /*No External Virtual*/

        SAMPLERTYPE_MAX,
    }

    public enum ETextureMipValueMode
    {
        /* Use hardware computed sample's mip level with automatic anisotropic filtering support. */
        TMVM_None,

        /* Explicitly compute the sample's mip level. Disables anisotropic filtering. */
        TMVM_MipLevel,

        /* Bias the hardware computed sample's mip level. Disables anisotropic filtering. */
        TMVM_MipBias,

        /* Explicitly compute the sample's DDX and DDY for anisotropic filtering. */
        TMVM_Derivative,

        TMVM_MAX,
    }

    public class UMaterialExpressionTextureBase : UMaterialExpression
    {
        public UTexture? Texture { get; private set; }
        public EMaterialSamplerType SamplerType { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            SamplerType = GetOrDefault<EMaterialSamplerType>(nameof(SamplerType));
            if (TryGetValue(out FPackageIndex objectPtr, "Texture") && objectPtr.TryLoad(out UTexture texture))
                Texture = texture;
        }
    }

    public class UMaterialExpressionMultiply : UMaterialExpression
    {
        public FExpressionInput? A { get; private set; }
        public FExpressionInput? B { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            A = GetOrDefault<FExpressionInput>(nameof(A));
            B = GetOrDefault<FExpressionInput>(nameof(B));
        }
    }

    public class UMaterialExpressionAdd : UMaterialExpression
    {
        public FExpressionInput? A { get; private set; }
        public FExpressionInput? B { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            A = GetOrDefault<FExpressionInput>(nameof(A));
            B = GetOrDefault<FExpressionInput>(nameof(B));
        }
    }

    public class UMaterialExpressionDepthBiasedAlpha : UMaterialExpression
    {
        public FExpressionInput? Alpha { get; private set; }
        public FExpressionInput? Bias { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            Alpha = GetOrDefault<FExpressionInput>(nameof(Alpha));
            Bias = GetOrDefault<FExpressionInput>(nameof(Bias));
        }
    }

    public class UMaterialExpressionPower : UMaterialExpression
    {
        public FExpressionInput? Base { get; private set; }
        public FExpressionInput? Exponent { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            Base = GetOrDefault<FExpressionInput>(nameof(Base));
            Exponent = GetOrDefault<FExpressionInput>(nameof(Exponent));
        }
    }

    public class UMaterialExpressionConstantClamp : UMaterialExpressionOneMinus;
    public class UMaterialExpressionOneMinus : UMaterialExpression
    {
        public FExpressionInput? Input { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            Input = GetOrDefault<FExpressionInput>(nameof(Input));
        }
    }

    public class FMaterialExpressionTextureBase : IUStruct
    {
        public FPackageIndex? Texture { get; private set; }

        public FMaterialExpressionTextureBase(FAssetArchive Ar)
        {
            if (Ar.Game < GAME_UE4_0)
            {
                // if less than EUnrealEngineObjectUE3Version.UNIFORM_EXPRESSIONS_IN_SHADER_CACHE it's a "TextureIndex"
                Texture = new FPackageIndex(Ar);
            }
        }
    }

    public class FMaterialUniformExpressionFlipBookTextureParameter : FMaterialExpressionTextureBase
    {
        public FMaterialUniformExpressionFlipBookTextureParameter(FAssetArchive Ar)
            : base(Ar)
        {
        }
    }

    public class UMaterialExpressionTextureSample : UMaterialExpressionTextureBase
    {
        public FExpressionInput? Coordinates { get; private set; }
        public FExpressionInput? TextureObject { get; private set; }
        public FExpressionInput? MipValue { get; private set; }
        public FExpressionInput? CoordinatesDX { get; private set; }
        public FExpressionInput? CoordinatesDY { get; private set; }
        public FExpressionInput? AutomaticViewMipBiasValue { get; private set; }
        public ETextureMipValueMode MipValueMode { get; private set; }
        public ESamplerSourceMode SamplerSource { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            Coordinates = GetOrDefault<FExpressionInput>(nameof(Coordinates));
            TextureObject = GetOrDefault<FExpressionInput>(nameof(TextureObject));
            MipValue = GetOrDefault<FExpressionInput>(nameof(MipValue));
            CoordinatesDX = GetOrDefault<FExpressionInput>(nameof(CoordinatesDX));
            CoordinatesDY = GetOrDefault<FExpressionInput>(nameof(CoordinatesDY));
            AutomaticViewMipBiasValue = GetOrDefault<FExpressionInput>(nameof(AutomaticViewMipBiasValue));
            MipValueMode = GetOrDefault<ETextureMipValueMode>(nameof(MipValueMode));
            SamplerSource = GetOrDefault<ESamplerSourceMode>(nameof(SamplerSource));
        }
    }

    public class UMaterialExpressionTextureSampleParameter : UMaterialExpressionTextureSample
    {
        public FName ParameterName { get; private set; }
        public FGuid ExpressionGUID { get; private set; }
        public FName Group { get; private set; }
        public FParameterChannelNames ChannelNames { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            ParameterName = GetOrDefault<FName>(nameof(ParameterName));
            ExpressionGUID = GetOrDefault<FGuid>(nameof(ExpressionGUID));
            Group = GetOrDefault<FName>(nameof(Group));
            ChannelNames = GetOrDefault<FParameterChannelNames>(nameof(ChannelNames));
        }
    }

    public class UMaterialExpressionTextureSampleParameter2D : UMaterialExpressionTextureSampleParameter { }
}
