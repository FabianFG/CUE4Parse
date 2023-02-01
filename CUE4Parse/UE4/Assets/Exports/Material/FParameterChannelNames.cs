using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.i18N;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    [StructFallback]
    public struct FParameterChannelNames : IUStruct
    {
        public readonly FText R;
        public readonly FText G;
        public readonly FText B;
        public readonly FText A;

        public FParameterChannelNames(FStructFallback fallback)
        {
            R = fallback.GetOrDefault<FText>(nameof(R));
            G = fallback.GetOrDefault<FText>(nameof(G));
            B = fallback.GetOrDefault<FText>(nameof(B));
            A = fallback.GetOrDefault<FText>(nameof(A));
        }

        public FParameterChannelNames(FAssetArchive Ar)
        {
            R = new FText(Ar);
            G = new FText(Ar);
            B = new FText(Ar);
            A = new FText(Ar);
        }
    }
}
