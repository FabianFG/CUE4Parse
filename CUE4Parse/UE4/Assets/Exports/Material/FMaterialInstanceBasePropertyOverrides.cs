using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    [StructFallback]
    public class FMaterialInstanceBasePropertyOverrides
    {
        public readonly EBlendMode BlendMode;
        public readonly EMaterialShadingModel ShadingModel;
        public readonly float OpacityMaskClipValue;
        public readonly bool DitheredLODTransition;

        public FMaterialInstanceBasePropertyOverrides(FStructFallback fallback)
        {
            BlendMode = fallback.GetOrDefault<EBlendMode>(nameof(BlendMode));
            ShadingModel = fallback.GetOrDefault<EMaterialShadingModel>(nameof(ShadingModel));
            OpacityMaskClipValue = fallback.GetOrDefault<float>(nameof(OpacityMaskClipValue));
            DitheredLODTransition = fallback.GetOrDefault<bool>(nameof(DitheredLODTransition));
        }
    }
}
