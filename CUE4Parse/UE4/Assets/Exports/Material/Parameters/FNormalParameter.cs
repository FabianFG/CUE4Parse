using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Material.Parameters
{
    [StructFallback]
    public class FNormalParameter : FStaticParameterBase
    {
        public TextureCompressionSettings CompressionSettings;

        public FNormalParameter(FArchive Ar) : base(Ar)
        {
            CompressionSettings = (TextureCompressionSettings)Ar.Read<byte>();
            bOverride = Ar.ReadBoolean();
            ExpressionGuid = Ar.Read<FGuid>();
        }

        public FNormalParameter(FStructFallback fallback) : base(fallback)
        {
            CompressionSettings = fallback.GetOrDefault<TextureCompressionSettings>(nameof(CompressionSettings));
        }
    }
}
