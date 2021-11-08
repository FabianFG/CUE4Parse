using CUE4Parse.UE4.Assets.Exports.Material.Parameters;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public class UMaterialInstance : UMaterialInterface
    {
        private ResolvedObject? _parent;
        public UUnrealMaterial? Parent => _parent?.Load<UUnrealMaterial>();
        public FMaterialInstanceBasePropertyOverrides BasePropertyOverrides;
        public FStaticParameterSet? StaticParameters;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            _parent = GetOrDefault<ResolvedObject>(nameof(Parent));
            BasePropertyOverrides = GetOrDefault<FMaterialInstanceBasePropertyOverrides>(nameof(BasePropertyOverrides));
            StaticParameters = GetOrDefault<FStaticParameterSet>(nameof(StaticParameters));

            var bHasStaticPermutationResource = GetOrDefault<bool>("bHasStaticPermutationResource");

            if (bHasStaticPermutationResource)
            {
                if (Ar.Ver >= EUnrealEngineObjectUE4Version.PURGED_FMATERIAL_COMPILE_OUTPUTS)
                {
                    if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MaterialAttributeLayerParameters)
                    {
                        StaticParameters = new FStaticParameterSet(Ar);
                    }

#if READ_SHADER_MAPS
                    DeserializeInlineShaderMaps(Ar, LoadedMaterialResources);
#else
                    Ar.Position = validPos;
#endif
                }
            }
        }
    }

    [StructFallback]
    public class FStaticParameterSet
    {
        public FStaticSwitchParameter[] StaticSwitchParameters;
        public FStaticComponentMaskParameter[] StaticComponentMaskParameters;
        public FStaticTerrainLayerWeightParameter[] TerrainLayerWeightParameters;
        public FStaticMaterialLayersParameter[] MaterialLayersParameters;

        public FStaticParameterSet(FArchive Ar)
        {
            StaticSwitchParameters = Ar.ReadArray(() => new FStaticSwitchParameter(Ar));
            StaticComponentMaskParameters = Ar.ReadArray(() => new FStaticComponentMaskParameter(Ar));
            TerrainLayerWeightParameters = Ar.ReadArray(() => new FStaticTerrainLayerWeightParameter(Ar));

            if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.MaterialLayersParameterSerializationRefactor)
            {
                MaterialLayersParameters = Ar.ReadArray(() => new FStaticMaterialLayersParameter(Ar));
            }
        }

        public FStaticParameterSet(FStructFallback fallback)
        {
            StaticSwitchParameters = fallback.GetOrDefault<FStaticSwitchParameter[]>(nameof(StaticSwitchParameters));
        }
    }
}
