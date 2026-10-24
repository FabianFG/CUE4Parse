using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    [StructFallback]
    public class FMaterialInstanceBasePropertyOverrides
    {
        public readonly EBlendMode BlendMode;
        public readonly EMaterialShadingModel ShadingModel;
        public readonly float OpacityMaskClipValue;
        public readonly bool DitheredLODTransition;

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

        public FMaterialInstanceBasePropertyOverrides(FStructFallback fallback)
        {
            BlendMode = fallback.GetOrDefault<EBlendMode>(nameof(BlendMode));
            ShadingModel = fallback.GetOrDefault<EMaterialShadingModel>(nameof(ShadingModel));
            OpacityMaskClipValue = fallback.GetOrDefault<float>(nameof(OpacityMaskClipValue));
            DitheredLODTransition = fallback.GetOrDefault<bool>(nameof(DitheredLODTransition));
        }
    }
}
