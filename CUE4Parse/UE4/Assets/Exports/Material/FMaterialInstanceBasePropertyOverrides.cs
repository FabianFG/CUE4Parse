using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    [StructFallback]
    public class FMaterialInstanceBasePropertyOverrides
    {
        public readonly bool bOverride_BlendMode;
        public readonly bool bOverride_OpacityMaskClipValue;
        public readonly bool bOverride_TwoSided;
        public readonly EBlendMode BlendMode;
        public readonly EMaterialShadingModel ShadingModel;
        public readonly float OpacityMaskClipValue;
        public readonly bool TwoSided;
        public readonly bool DitheredLODTransition;

        /*
        // untested
        public FMaterialInstanceBasePropertyOverrides(FAssetArchive Ar)
        {
            Ar.ReadBoolean(); // bOverrideBaseProperties_DEPRECATED
            var bHasPropertyOverrides = Ar.ReadBoolean();
            if (bHasPropertyOverrides)
            {
                Ar.ReadBoolean(); // bOverride_OpacityMaskClipValue
                Ar.ReadBoolean(); // OpacityMaskClipValue

                if (Ar.Ver >= EUnrealEngineObjectUE4Version.MATERIAL_INSTANCE_BASE_PROPERTY_OVERRIDES_PHASE_2)
                {
                    Ar.ReadBoolean(); // bOverride_BlendMode
                    Ar.ReadBoolean(); // BlendMode
                    Ar.ReadBoolean(); // bOverride_ShadingModel
                    Ar.ReadBoolean(); // ShadingModel
                    Ar.ReadBoolean(); // bOverride_TwoSided
                    Ar.ReadBoolean(); // bTwoSided
                }

                if (Ar.Ver >= EUnrealEngineObjectUE4Version.MATERIAL_INSTANCE_BASE_PROPERTY_OVERRIDES_DITHERED_LOD_TRANSITION)
                {
                    Ar.ReadBoolean(); // bOverride_DitheredLODTransition
                    Ar.ReadBoolean(); // bDitheredLODTransition
                }
            }
        }
         */

        public FMaterialInstanceBasePropertyOverrides(FStructFallback fallback)
        {
            bOverride_BlendMode = fallback.GetOrDefault<bool>(nameof(bOverride_BlendMode));
            bOverride_OpacityMaskClipValue = fallback.GetOrDefault<bool>(nameof(bOverride_OpacityMaskClipValue));
            bOverride_TwoSided = fallback.GetOrDefault<bool>(nameof(bOverride_TwoSided));
            BlendMode = fallback.GetOrDefault<EBlendMode>(nameof(BlendMode));
            ShadingModel = fallback.GetOrDefault<EMaterialShadingModel>(nameof(ShadingModel));
            OpacityMaskClipValue = fallback.GetOrDefault<float>(nameof(OpacityMaskClipValue));
            TwoSided = fallback.GetOrDefault<bool>(nameof(TwoSided));
            DitheredLODTransition = fallback.GetOrDefault<bool>(nameof(DitheredLODTransition));
        }
    }
}
