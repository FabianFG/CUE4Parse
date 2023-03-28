using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Material.Parameters
{
    [StructFallback]
    public class FStaticComponentMaskParameter : FStaticParameterBase
    {
        public bool R, G, B, A;

        public FStaticComponentMaskParameter(FStructFallback fallback) : base(fallback)
        {
            R = fallback.GetOrDefault<bool>(nameof(R));
            G = fallback.GetOrDefault<bool>(nameof(G));
            B = fallback.GetOrDefault<bool>(nameof(B));
            A = fallback.GetOrDefault<bool>(nameof(A));
        }

        public FStaticComponentMaskParameter(FArchive Ar) : base(Ar)
        {
            R = Ar.ReadBoolean();
            G = Ar.ReadBoolean();
            B = Ar.ReadBoolean();
            A = Ar.ReadBoolean();
            bOverride = Ar.ReadBoolean();
            ExpressionGuid = Ar.Read<FGuid>();
        }
    }
}