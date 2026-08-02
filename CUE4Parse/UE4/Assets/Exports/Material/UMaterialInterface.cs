using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Material;

[SkipObjectRegistration]
public class UMaterialInterface : UUnrealMaterial
{
    public bool bUseMobileSpecular;
    public float MobileSpecularPower = 16.0f;
    public EMobileSpecularMask MobileSpecularMask = EMobileSpecularMask.MSM_Constant;
    public FPackageIndex? FlattenedTexture;
    public FPackageIndex? MobileBaseTexture;
    public FPackageIndex? MobileNormalTexture;
    public FPackageIndex? MobileMaskTexture;

    public FStructFallback? CachedExpressionData;
    public FMaterialTextureInfo[] TextureStreamingData = Array.Empty<FMaterialTextureInfo>();
    public List<FMaterialResource> LoadedMaterialResources = new();

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if(Ar.Game == GAME_WorldofJadeDynasty) Ar.Position += 24;
        base.Deserialize(Ar, validPos);
        bUseMobileSpecular = GetOrDefault<bool>(nameof(bUseMobileSpecular));
        MobileSpecularPower = GetOrDefault<float>(nameof(MobileSpecularPower));
        MobileSpecularMask = GetOrDefault<EMobileSpecularMask>(nameof(MobileSpecularMask));
        FlattenedTexture = GetOrDefault<FPackageIndex?>(nameof(FlattenedTexture));
        MobileBaseTexture = GetOrDefault<FPackageIndex?>(nameof(MobileBaseTexture));
        MobileNormalTexture = GetOrDefault<FPackageIndex?>(nameof(MobileNormalTexture));
        MobileMaskTexture = GetOrDefault<FPackageIndex?>(nameof(MobileMaskTexture));
        TextureStreamingData = GetOrDefault(nameof(TextureStreamingData), Array.Empty<FMaterialTextureInfo>());

        var bSavedCachedExpressionData = FUE5ReleaseStreamObjectVersion.Get(Ar) >= FUE5ReleaseStreamObjectVersion.Type.MaterialInterfaceSavedCachedData && Ar.ReadBoolean();
        if (bSavedCachedExpressionData)
        {
            CachedExpressionData = new FStructFallback(Ar, "MaterialCachedExpressionData");
        }

        if (Ar.Game == GAME_HogwartsLegacy) CustomGameData = new FSHAHash(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (LoadedMaterialResources is not null)
        {
            writer.WritePropertyName("LoadedMaterialResources");
            serializer.Serialize(writer, LoadedMaterialResources);
        }

        if (CachedExpressionData is not null)
        {
            writer.WritePropertyName("CachedExpressionData");
            serializer.Serialize(writer, CachedExpressionData);
        }

    }

    public override void GetParams(CMaterialParams parameters)
    {
        if (FlattenedTexture?.TryLoad<UTexture>(out var flattenedTexture) == true) parameters.Diffuse = flattenedTexture;
        if (MobileBaseTexture?.TryLoad<UTexture>(out var mobileBaseTexture) == true) parameters.Diffuse = mobileBaseTexture;
        if (MobileNormalTexture?.TryLoad<UTexture>(out var mobileNormalTexture) == true) parameters.Normal = mobileNormalTexture;
        if (MobileMaskTexture?.TryLoad<UTexture>(out var mobileMaskTexture) == true) parameters.Opacity = mobileMaskTexture;
        parameters.UseMobileSpecular = bUseMobileSpecular;
        parameters.MobileSpecularPower = MobileSpecularPower;
        parameters.MobileSpecularMask = MobileSpecularMask;
    }

    public override void GetParams(CMaterialParams2 parameters, EMaterialDepth depth)
    {
        if (FlattenedTexture?.TryLoad<UTexture>(out var flattenedTexture) == true)
            parameters.VerifyTexture("Diffuse", flattenedTexture, false);
        if (MobileBaseTexture?.TryLoad<UTexture>(out var mobileBaseTexture) == true)
            parameters.VerifyTexture("Diffuse", mobileBaseTexture, false);
        if (MobileNormalTexture?.TryLoad<UTexture>(out var mobileNormalTexture) == true)
            parameters.VerifyTexture("Normal", mobileNormalTexture, false);

        for (int i = 0; i < TextureStreamingData.Length; i++)
        {
            var name = TextureStreamingData[i].TextureName.Text;
            if (!parameters.TryGetTexture2d(out var texture, name))
                continue;

            parameters.VerifyTexture(name, texture, false);
        }

        // *****************************************
        // CachedExpressionData ONLY AFTER THIS LINE
        // *****************************************

        if (CachedExpressionData == null) return;
        if (CachedExpressionData.TryGetValue(out FStructFallback materialParameters, "Parameters"))
        {
            ParseCachedDataLegacy(parameters, materialParameters);
        }
        else
        {
            ParseCachedData(parameters, CachedExpressionData);
        }
    }

    private void ParseCachedDataLegacy(CMaterialParams2 parameters, FStructFallback materialParameters)
    {
        if (!materialParameters.TryGetAllValues(out FStructFallback[] runtimeEntries, "RuntimeEntries"))
            return;

        if (materialParameters.TryGetValue(out float[] scalarValues, "ScalarValues") &&
            runtimeEntries.Length > 0 &&
            runtimeEntries[0].TryGetValue(out FMaterialParameterInfo[] scalarParameterInfos, "ParameterInfos"))
            for (int i = 0; i < scalarParameterInfos.Length; i++)
                parameters.Scalars[scalarParameterInfos[i].Name.Text] = scalarValues[i];

        if (materialParameters.TryGetValue(out FLinearColor[] vectorValues, "VectorValues") &&
            runtimeEntries.Length > 1 &&
            runtimeEntries[1].TryGetValue(out FMaterialParameterInfo[] vectorParameterInfos, "ParameterInfos"))
            for (int i = 0; i < vectorParameterInfos.Length; i++)
                parameters.Colors[vectorParameterInfos[i].Name.Text] = vectorValues[i];

        if (materialParameters.TryGetValue(out FPackageIndex[] textureValues, "TextureValues") &&
            runtimeEntries.Length > 2 &&
            runtimeEntries[2].TryGetValue(out FMaterialParameterInfo[] textureParameterInfos, "ParameterInfos"))
        {
            for (int i = 0; i < textureParameterInfos.Length; i++)
            {
                var name = textureParameterInfos[i].Name.Text;
                if (!textureValues[i].TryLoad(out UTexture texture)) continue;

                parameters.VerifyTexture(name, texture);
            }
        }
    }

    private void ParseCachedData(CMaterialParams2 parameters, FStructFallback materialParameters)
    {
        if (!materialParameters.TryGetAllValues(out FStructFallback[] runtimeEntries, "RuntimeEntries"))
            return;

        if (materialParameters.TryGetValue(out float[] scalarValues, "ScalarValues") &&
            runtimeEntries.Length > 0 &&
            runtimeEntries[0].TryGetValue(out FMaterialParameterInfo[] scalarParameterInfos, "ParameterInfoSet"))
            for (int i = 0; i < scalarParameterInfos.Length; i++)
                parameters.Scalars[scalarParameterInfos[i].Name.Text] = scalarValues[i];

        if (materialParameters.TryGetValue(out FLinearColor[] vectorValues, "VectorValues") &&
            runtimeEntries.Length > 1 &&
            runtimeEntries[1].TryGetValue(out FMaterialParameterInfo[] vectorParameterInfos, "ParameterInfoSet"))
            for (int i = 0; i < vectorParameterInfos.Length; i++)
                parameters.Colors[vectorParameterInfos[i].Name.Text] = vectorValues[i];

        if (materialParameters.TryGetValue(out FSoftObjectPath[] textureValues, "TextureValues") &&
            runtimeEntries.Length > 3 &&
            runtimeEntries[3].TryGetValue(out FMaterialParameterInfo[] textureParameterInfos, "ParameterInfoSet"))
        {
            for (int i = 0; i < textureParameterInfos.Length; i++)
            {
                var name = textureParameterInfos[i].Name.Text;
                if (!textureValues[i].TryLoad(out UTexture texture)) continue;

                parameters.VerifyTexture(name, texture);
            }
        }
    }

    public void DeserializeInlineShaderMaps(FAssetArchive Ar, ICollection<FMaterialResource> loadedResources)
    {
        var numLoadedResources = Ar.Read<int>();
        if (numLoadedResources > 0)
        {
            FMaterialResourceProxyReader resourceAr;
            if (Ar.Game != GAME_Stalker2)
            {
                resourceAr = new FMaterialResourceProxyReader(Ar);
            }
            else
            {
                var ShaderMaps = new FByteBulkData(Ar);
                using var ShaderMapsAr = new FByteArchive("ShaderMaps", ShaderMaps.Data, Ar.Versions);
                resourceAr = new FMaterialResourceProxyReader(ShaderMapsAr);
            }

            for (var resourceIndex = 0; resourceIndex < numLoadedResources; ++resourceIndex)
            {
                var loadedResource = new FMaterialResource();
                loadedResource.DeserializeInlineShaderMap(resourceAr);
                loadedResources.Add(loadedResource);
            }
        }
    }
}
