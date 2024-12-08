using System;
using CUE4Parse.UE4.Assets.Exports.Material.Parameters;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public class UMaterialInstance : UMaterialInterface
{
    private ResolvedObject? _parent;
    public UUnrealMaterial? Parent => _parent?.Load<UUnrealMaterial>();
    public bool bHasStaticPermutationResource;
    public FMaterialInstanceBasePropertyOverrides? BasePropertyOverrides;
    public FStaticParameterSet? StaticParameters;
    public FStructFallback? CachedData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        _parent = GetOrDefault<ResolvedObject>(nameof(Parent));
        bHasStaticPermutationResource = GetOrDefault<bool>("bHasStaticPermutationResource");
        BasePropertyOverrides = GetOrDefault<FMaterialInstanceBasePropertyOverrides>(nameof(BasePropertyOverrides));
        StaticParameters = GetOrDefault(nameof(StaticParameters), GetOrDefault<FStaticParameterSet>("StaticParametersRuntime"));

        var bSavedCachedData = FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.MaterialSavedCachedData && Ar.ReadBoolean();
        if (bSavedCachedData)
        {
            CachedData = new FStructFallback(Ar, "MaterialInstanceCachedData");
        }

        if (bHasStaticPermutationResource && Ar.Ver >= EUnrealEngineObjectUE4Version.PURGED_FMATERIAL_COMPILE_OUTPUTS)
        {
            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MaterialAttributeLayerParameters)
            {
                StaticParameters = new FStaticParameterSet(Ar);
            }

            if (Ar.Game >= EGame.GAME_UE4_25 && Ar.Owner?.Provider?.ReadShaderMaps == true)
            {
                try
                {
                    DeserializeInlineShaderMaps(Ar, LoadedMaterialResources);
                }
                finally
                {
                    Ar.Position = validPos;
                }
            }
            else
            {
                Ar.Position = validPos;
            }
        }
    }

    public override void GetParams(CMaterialParams2 parameters, EMaterialFormat format)
    {
        base.GetParams(parameters, format);

        if (StaticParameters != null)
            foreach (var switchParameter in StaticParameters.StaticSwitchParameters)
                parameters.Switches[switchParameter.Name] = switchParameter.Value;

        if (BasePropertyOverrides != null)
        {
            parameters.BlendMode = BasePropertyOverrides.BlendMode;
            parameters.ShadingModel = BasePropertyOverrides.ShadingModel;
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (CachedData != null)
        {
            writer.WritePropertyName("CachedData");
            serializer.Serialize(writer, CachedData);
        }
    }
}

[StructFallback]
public class FStaticParameterSet
{
    public FStaticSwitchParameter[] StaticSwitchParameters;
    public FStaticComponentMaskParameter[] StaticComponentMaskParameters;
    public FStaticTerrainLayerWeightParameter[] TerrainLayerWeightParameters;
    public FStaticMaterialLayersParameter[]? MaterialLayersParameters;

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
        StaticSwitchParameters = fallback.GetOrDefault(nameof(StaticSwitchParameters), Array.Empty<FStaticSwitchParameter>());
        StaticComponentMaskParameters = fallback.GetOrDefault(nameof(StaticComponentMaskParameters), Array.Empty<FStaticComponentMaskParameter>());
        TerrainLayerWeightParameters = fallback.GetOrDefault(nameof(TerrainLayerWeightParameters), Array.Empty<FStaticTerrainLayerWeightParameter>());
        MaterialLayersParameters = fallback.GetOrDefault(nameof(MaterialLayersParameters), Array.Empty<FStaticMaterialLayersParameter>());
    }
}
