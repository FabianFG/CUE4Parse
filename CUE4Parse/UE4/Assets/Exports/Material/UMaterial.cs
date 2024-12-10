using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public class UMaterial : UMaterialInterface
{
    public bool TwoSided { get; private set; }
    public bool bDisableDepthTest { get; private set; }
    public bool bIsMasked { get; private set; }
    public FPackageIndex[] Expressions { get; private set; } = [];
    public EBlendMode BlendMode { get; private set; } = EBlendMode.BLEND_Opaque;
    public ETranslucencyLightingMode TranslucencyLightingMode { get; private set; } = ETranslucencyLightingMode.TLM_VolumetricNonDirectional;
    public EMaterialShadingModel ShadingModel { get; private set; } = EMaterialShadingModel.MSM_Unlit;
    public float OpacityMaskClipValue { get; private set; } = 0.333f;
    public List<UTexture> ReferencedTextures { get; } = [];

    private readonly List<IObject> _displayedReferencedTextures = [];
    private bool _shouldDisplay;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        TwoSided = GetOrDefault<bool>(nameof(TwoSided));
        bDisableDepthTest = GetOrDefault<bool>(nameof(bDisableDepthTest));
        bIsMasked = GetOrDefault<bool>(nameof(bIsMasked));
        Expressions = GetOrDefault(nameof(Expressions), Expressions);
        BlendMode = GetOrDefault(nameof(BlendMode), BlendMode);
        TranslucencyLightingMode = GetOrDefault(nameof(TranslucencyLightingMode), TranslucencyLightingMode);
        ShadingModel = GetOrDefault(nameof(ShadingModel), ShadingModel);
        OpacityMaskClipValue = GetOrDefault(nameof(OpacityMaskClipValue), OpacityMaskClipValue);

        // 4.25+
        if (Ar.Game >= EGame.GAME_UE4_25)
        {
            CachedExpressionData ??= GetOrDefault<FStructFallback>(nameof(CachedExpressionData));
            if (CachedExpressionData != null && CachedExpressionData.TryGetValue(out UTexture[] referencedTextures, "ReferencedTextures"))
                ReferencedTextures.AddRange(referencedTextures);

            if (TryGetValue(out referencedTextures, "ReferencedTextures")) // is this a thing ?
                ReferencedTextures.AddRange(referencedTextures);
        }

        // UE4 has complex FMaterialResource format, so avoid reading anything here, but
        // scan package's imports for UTexture objects instead
        if (Ar is { Game: >= EGame.GAME_UE5_0, Owner.Provider.SkipReferencedTextures: false })
            ScanForTextures(Ar);

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.PURGED_FMATERIAL_COMPILE_OUTPUTS)
        {
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

    public UTexture? GetFirstTexture() => ReferencedTextures.Count > 0 ? ReferencedTextures[0] : null;
    public UTexture? GetTextureAtIndex(int index) => ReferencedTextures.Count >= index ? ReferencedTextures[index] : null;

    private void ScanForTextures(FAssetArchive Ar)
    {
        // !! NOTE: this code will not work when textures are located in the same package - they don't present in import table
        // !! but could be found in export table. That's true for Simplygon-generated materials.
        switch (Ar.Owner)
        {
            case IoPackage io:
            {
                foreach (var import in io.ImportMap)
                {
                    var resolved = io.ResolveObjectIndex(import);
                    if (resolved?.Class == null) continue;

                    if (!resolved.Class.Name.Text.StartsWith("Texture", StringComparison.OrdinalIgnoreCase) ||
                        !resolved.TryLoad(out var tex) || tex is not UTexture texture) continue;

                    _displayedReferencedTextures.Add(resolved);
                    ReferencedTextures.Add(texture);
                }
                break;
            }
            case Package pak: // ue5?
            {
                for (var i = 0; i < pak.ImportMap.Length; i++)
                {
                    if (!pak.ImportMap[i].ClassName.Text.StartsWith("Texture", StringComparison.OrdinalIgnoreCase)) continue;
                    var resolved = pak.ResolvePackageIndex(new FPackageIndex(Ar, -i - 1));
                    if (resolved?.Class == null || !resolved.TryLoad(out var tex) || tex is not UTexture texture) continue;

                    _displayedReferencedTextures.Add(resolved);
                    ReferencedTextures.Add(texture);
                }
                break;
            }
        }
        _shouldDisplay = _displayedReferencedTextures.Count > 0;
    }

    public override void GetParams(CMaterialParams parameters)
    {
        base.GetParams(parameters);

        var diffWeight = 0;
        var normWeight = 0;
        var specWeight = 0;
        var specPowWeight = 0;
        var opWeight = 0;
        var emWeight = 0;

        void Diffuse(bool check, int weight, UTexture tex)
        {
            if (check && weight > diffWeight)
            {
                parameters.Diffuse = tex;
                diffWeight = weight;
            }
        }

        void Normal(bool check, int weight, UTexture tex)
        {
            if (check && weight > normWeight)
            {
                parameters.Normal = tex;
                normWeight = weight;
            }
        }

        void Specular(bool check, int weight, UTexture tex)
        {
            if (check && weight > specWeight)
            {
                parameters.Specular = tex;
                specWeight = weight;
            }
        }

        void SpecPower(bool check, int weight, UTexture tex)
        {
            if (check && weight > specPowWeight)
            {
                parameters.SpecPower = tex;
                specPowWeight = weight;
            }
        }

        void Opacity(bool check, int weight, UTexture tex)
        {
            if (check && weight > opWeight)
            {
                parameters.Opacity = tex;
                opWeight = weight;
            }
        }

        void Emissive(bool check, int weight, UTexture tex)
        {
            if (check && weight > emWeight)
            {
                parameters.Emissive = tex;
                emWeight = weight;
            }
        }

        for (var i = 0; i < ReferencedTextures.Count; i++)
        {
            var tex = ReferencedTextures[i];
            if (tex == null) continue;

            var name = tex.Name;
            if (name.Contains("noise", StringComparison.CurrentCultureIgnoreCase)) continue;
            if (name.Contains("detail", StringComparison.CurrentCultureIgnoreCase)) continue;

            Diffuse(name.Contains("diff", StringComparison.CurrentCultureIgnoreCase), 100, tex);
            Normal(name.Contains("norm", StringComparison.CurrentCultureIgnoreCase), 100, tex);
            Diffuse(name.EndsWith("_Tex", StringComparison.CurrentCultureIgnoreCase), 80, tex);
            Diffuse(name.Contains("_Tex", StringComparison.CurrentCultureIgnoreCase), 60, tex);
            Diffuse(name.Contains("_D", StringComparison.CurrentCultureIgnoreCase), 20, tex);
            Opacity(name.Contains("_OM", StringComparison.CurrentCultureIgnoreCase), 20, tex);

            Diffuse(name.Contains("_DI", StringComparison.CurrentCultureIgnoreCase), 20, tex);
            Diffuse(name.Contains("_D", StringComparison.CurrentCultureIgnoreCase), 11, tex);
            Diffuse(name.Contains("_Albedo", StringComparison.CurrentCultureIgnoreCase), 19, tex);
            Diffuse(name.EndsWith("_C", StringComparison.CurrentCultureIgnoreCase), 10, tex);
            Diffuse(name.EndsWith("_CM", StringComparison.CurrentCultureIgnoreCase), 12, tex);
            Normal(name.EndsWith("_N", StringComparison.CurrentCultureIgnoreCase), 20, tex);
            Normal(name.EndsWith("_NM", StringComparison.CurrentCultureIgnoreCase), 20, tex);
            Normal(name.Contains("_N", StringComparison.CurrentCultureIgnoreCase), 9, tex);

            Specular(name.EndsWith("_S", StringComparison.CurrentCultureIgnoreCase), 20, tex);
            Specular(name.Contains("_S_", StringComparison.CurrentCultureIgnoreCase), 15, tex);
            SpecPower(name.EndsWith("_SP", StringComparison.CurrentCultureIgnoreCase), 20, tex);
            SpecPower(name.EndsWith("_SM", StringComparison.CurrentCultureIgnoreCase), 20, tex);
            SpecPower(name.Contains("_SP", StringComparison.CurrentCultureIgnoreCase), 9, tex);
            Emissive(name.EndsWith("_E", StringComparison.CurrentCultureIgnoreCase), 20, tex);
            Emissive(name.EndsWith("_EM", StringComparison.CurrentCultureIgnoreCase), 21, tex);
            Opacity(name.EndsWith("_A", StringComparison.CurrentCultureIgnoreCase), 20, tex);
            if (bIsMasked)
                Opacity(name.EndsWith("_Mask", StringComparison.CurrentCultureIgnoreCase), 2, tex);

            Diffuse(name.StartsWith("df_", StringComparison.CurrentCultureIgnoreCase), 20, tex);
            Specular(name.StartsWith("sp_", StringComparison.CurrentCultureIgnoreCase), 20, tex);
            Normal(name.StartsWith("no_", StringComparison.CurrentCultureIgnoreCase), 20, tex);

            Normal(name.Contains("Norm", StringComparison.CurrentCultureIgnoreCase), 80, tex);
            Emissive(name.Contains("Emis", StringComparison.CurrentCultureIgnoreCase), 80, tex);
            Specular(name.Contains("Specular", StringComparison.CurrentCultureIgnoreCase), 80, tex);
            Opacity(name.Contains("Opac", StringComparison.CurrentCultureIgnoreCase), 80, tex);
            Opacity(name.Contains("Alpha", StringComparison.CurrentCultureIgnoreCase), 100, tex);

            Diffuse(i == 0, 1, tex); // 1st texture as lowest weight
        }

        // do not allow normal map became a diffuse
        if (parameters.Diffuse == parameters.Normal && diffWeight < normWeight ||
            parameters.Diffuse is { IsTextureCube: true })
        {
            parameters.Diffuse = null;
        }
    }
    public override void GetParams(CMaterialParams2 parameters, EMaterialFormat format)
    {
        parameters.BlendMode = BlendMode;
        parameters.ShadingModel = ShadingModel;
        parameters.AppendAllProperties(Properties);

        foreach (var expression in Expressions)
        {
            if (!expression.TryLoad(out UMaterialExpression materialExpression))
                continue;

            switch (materialExpression)
            {
                case UMaterialExpressionTextureSampleParameter { Texture: not null } textureSample:
                    parameters.VerifyTexture(textureSample.ParameterName.Text, textureSample.Texture, true, textureSample.SamplerType);
                    break;
                case UMaterialExpressionTextureBase { Texture: not null } textureBase:
                    parameters.VerifyTexture(textureBase.Texture.Name, textureBase.Texture, true, textureBase.SamplerType);
                    break;
                case UMaterialExpressionVectorParameter vectorParameter:
                    parameters.Colors[vectorParameter.ParameterName.Text] = vectorParameter.DefaultValue;
                    break;
                case UMaterialExpressionScalarParameter scalarParameter:
                    parameters.Scalars[scalarParameter.ParameterName.Text] = scalarParameter.DefaultValue;
                    break;
                case UMaterialExpressionStaticBoolParameter staticBoolParameter:
                    parameters.Switches[staticBoolParameter.ParameterName.Text] = staticBoolParameter.DefaultValue;
                    break;
            }
        }

        if (format != EMaterialFormat.AllLayersNoRef)
        {
            for (int i = 0; i < ReferencedTextures.Count; i++)
            {
                if (ReferencedTextures[i] is not { } texture) continue;
                parameters.Textures[texture.Name] = texture;
            }
        }

        base.GetParams(parameters, format);
        if (format == EMaterialFormat.AllLayersNoRef) return;

        if (ReferencedTextures.Count == 1 && ReferencedTextures[0] is { } fallback)
        {
            parameters.Textures[CMaterialParams2.FallbackDiffuse] = fallback;
            return;
        }

        var textureIndex = ReferencedTextures.Count;
        while (!(
                   parameters.Textures.ContainsKey(CMaterialParams2.FallbackDiffuse) &&
                   parameters.Textures.ContainsKey(CMaterialParams2.FallbackNormals) &&
                   parameters.Textures.ContainsKey(CMaterialParams2.FallbackSpecularMasks) &&
                   parameters.Textures.ContainsKey(CMaterialParams2.FallbackEmissive))
               && textureIndex > 0)
        {
            textureIndex--;
            if (ReferencedTextures[textureIndex] is not { } texture) continue;

            if (!parameters.Textures.ContainsKey(CMaterialParams2.FallbackDiffuse) &&
                Regex.IsMatch(texture.Name, CMaterialParams2.RegexDiffuse, RegexOptions.IgnoreCase))
            {
                parameters.Textures[CMaterialParams2.FallbackDiffuse] = texture;
                continue;
            }

            if (!parameters.Textures.ContainsKey(CMaterialParams2.FallbackNormals) &&
                Regex.IsMatch(texture.Name, CMaterialParams2.RegexNormals, RegexOptions.IgnoreCase))
            {
                parameters.Textures[CMaterialParams2.FallbackNormals] = texture;
                continue;
            }

            if (!parameters.Textures.ContainsKey(CMaterialParams2.FallbackSpecularMasks) &&
                Regex.IsMatch(texture.Name, CMaterialParams2.RegexSpecularMasks, RegexOptions.IgnoreCase))
            {
                parameters.Textures[CMaterialParams2.FallbackSpecularMasks] = texture;
                continue;
            }

            if (!parameters.Textures.ContainsKey(CMaterialParams2.FallbackEmissive) &&
                Regex.IsMatch(texture.Name, CMaterialParams2.RegexEmissive, RegexOptions.IgnoreCase))
            {
                parameters.Textures[CMaterialParams2.FallbackEmissive] = texture;
                continue;
            }
        }
    }

    public override void AppendReferencedTextures(IList<UUnrealMaterial> outTextures, bool onlyRendered)
    {
        if (onlyRendered)
        {
            base.AppendReferencedTextures(outTextures, onlyRendered);
        }
        else
        {
            foreach (var texture in ReferencedTextures.Where(texture => !outTextures.Contains(texture)))
            {
                if (texture == null) continue;
                outTextures.Add(texture);
            }
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (!_shouldDisplay) return;
        writer.WritePropertyName("ReferencedTextures");
        serializer.Serialize(writer, _displayedReferencedTextures);
    }
}
