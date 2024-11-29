using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public class ULandscapeMaterialInstanceConstant: UMaterialInstanceConstant;

public class UMaterialInstanceConstant : UMaterialInstance
{
    public FScalarParameterValue[] ScalarParameterValues = [];
    public FTextureParameterValue[] TextureParameterValues = [];
    public FVectorParameterValue[] VectorParameterValues = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        ScalarParameterValues = GetOrDefault(nameof(ScalarParameterValues), Array.Empty<FScalarParameterValue>());
        TextureParameterValues = GetOrDefault(nameof(TextureParameterValues), Array.Empty<FTextureParameterValue>());
        VectorParameterValues = GetOrDefault(nameof(VectorParameterValues), Array.Empty<FVectorParameterValue>());
    }

    public override void GetParams(CMaterialParams parameters)
    {
        // get params from linked UMaterial3
        if (Parent != null && Parent != this)
            Parent.GetParams(parameters);

        base.GetParams(parameters);

        // get local parameters
        var diffWeight = 0;
        var normWeight = 0;
        var specWeight = 0;
        var specPowWeight = 0;
        var opWeight = 0;
        var emWeight = 0;
        var dcWeight = 0;
        var emcWeight = 0;
        var cubeWeight = 0;
        var maskWeight = 0;
        var miscWeight = 0;
        var metalWeight = 0;
        var roughWeight = 0;
        var specuWeight = 0;

        void Diffuse(bool check, int weight, UTexture tex)
        {
            if (check && weight >= diffWeight)
            {
                parameters.HasTopDiffuseTexture = true;
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
                parameters.HasTopEmissiveTexture = true;
                parameters.Emissive = tex;
                emWeight = weight;
            }
        }

        void CubeMap(bool check, int weight, UTexture tex)
        {
            if (check && weight > cubeWeight)
            {
                parameters.Cube = tex;
                cubeWeight = weight;
            }
        }

        void BakedMask(bool check, int weight, UTexture tex)
        {
            if (check && weight > maskWeight)
            {
                parameters.Mask = tex;
                maskWeight = weight;
            }
        }

        void Misc(bool check, int weight, UTexture tex)
        {
            if (check && weight > miscWeight)
            {
                parameters.Misc = tex;
                miscWeight = weight;
            }
        }

        void DiffuseColor(bool check, int weight, FLinearColor color)
        {
            if (check && weight > dcWeight)
            {
                parameters.DiffuseColor = color.ToSRGB();
                dcWeight = weight;
            }
        }

        void EmissiveColor(bool check, int weight, FLinearColor color)
        {
            if (check && weight > emcWeight)
            {
                parameters.EmissiveColor = color.ToSRGB();
                emcWeight = weight;
            }
        }

        void MetallicValue(bool check, int weight, float value)
        {
            if (check && weight > metalWeight)
            {
                parameters.MetallicValue = value;
                metalWeight = weight;
            }
        }

        void RoughnessValue(bool check, int weight, float value)
        {
            if (check && weight > roughWeight)
            {
                parameters.RoughnessValue = value;
                roughWeight = weight;
            }
        }

        void SpecularValue(bool check, int weight, float value)
        {
            if (check && weight > specuWeight)
            {
                parameters.SpecularValue = value;
                specuWeight = weight;
            }
        }

        if (TextureParameterValues.Length > 0)
            parameters.Opacity = null; // it's better to disable opacity mask from parent material

        foreach (var p in TextureParameterValues)
        {
            var name = p.Name;
            var tex = p.ParameterValue.Load<UTexture>();
            if (tex == null) continue;

            if (name.Contains("detail", StringComparison.CurrentCultureIgnoreCase) ||
                name.Contains("ws ", StringComparison.CurrentCultureIgnoreCase) ||
                name.Contains("_2", StringComparison.CurrentCultureIgnoreCase)) continue;

            Diffuse(name.Contains("dif", StringComparison.CurrentCultureIgnoreCase), 100, tex);
            Diffuse(name.Contains("albedo", StringComparison.CurrentCultureIgnoreCase), 100, tex);
            Diffuse(name.Contains("color", StringComparison.CurrentCultureIgnoreCase), 80, tex);
            Diffuse(name.Equals("co", StringComparison.CurrentCultureIgnoreCase), 70, tex);
            Diffuse(name.StartsWith("co_", StringComparison.CurrentCultureIgnoreCase), 70, tex);
            Normal(name.Contains("norm", StringComparison.CurrentCultureIgnoreCase) && !name.Contains("fx", StringComparison.CurrentCultureIgnoreCase), 100, tex);
            Normal(name.Equals("nm", StringComparison.CurrentCultureIgnoreCase), 70, tex);
            Normal(name.StartsWith("nm_", StringComparison.CurrentCultureIgnoreCase), 70, tex);
            SpecPower(name.Contains("specpow", StringComparison.CurrentCultureIgnoreCase), 100, tex);
            Specular(name.Contains("spec", StringComparison.CurrentCultureIgnoreCase), 100, tex);
            Specular(name.Contains("packed", StringComparison.CurrentCultureIgnoreCase), 80, tex);
            Specular(name.Contains("mrae", StringComparison.CurrentCultureIgnoreCase), 80, tex);
            Specular(name.Contains("mrs", StringComparison.CurrentCultureIgnoreCase), 80, tex);
            Specular(name.Equals("lp", StringComparison.CurrentCultureIgnoreCase), 70, tex);
            Specular(name.StartsWith("lp_", StringComparison.CurrentCultureIgnoreCase), 70, tex);
            Emissive(name.Contains("emiss", StringComparison.CurrentCultureIgnoreCase) && !name.Contains("gradient", StringComparison.CurrentCultureIgnoreCase), 100, tex);
            BakedMask(name.Contains("fx", StringComparison.CurrentCultureIgnoreCase) && name.Contains("mask", StringComparison.CurrentCultureIgnoreCase), 100, tex);
            CubeMap(name.Contains("cube", StringComparison.CurrentCultureIgnoreCase), 100, tex);
            CubeMap(name.Contains("refl", StringComparison.CurrentCultureIgnoreCase), 90, tex);
            Opacity(name.Contains("opac", StringComparison.CurrentCultureIgnoreCase), 90, tex);
            Opacity(name.Contains("trans", StringComparison.CurrentCultureIgnoreCase) && !name.Contains("transm", StringComparison.CurrentCultureIgnoreCase), 80, tex);
            Opacity(name.Contains("opacity", StringComparison.CurrentCultureIgnoreCase), 100, tex);
            Opacity(name.Contains("alpha", StringComparison.CurrentCultureIgnoreCase), 100, tex);
            Misc(name.Equals("m", StringComparison.CurrentCultureIgnoreCase), 100, tex);
        }

        foreach (var p in VectorParameterValues)
        {
            var name = p.Name;
            var color = p.ParameterValue;
            if (color == null) continue;

            DiffuseColor(name.Contains("color", StringComparison.CurrentCultureIgnoreCase), 100, color.Value);
            DiffuseColor(name.Equals("co", StringComparison.CurrentCultureIgnoreCase), 80, color.Value);
            EmissiveColor(name.Contains("emis", StringComparison.CurrentCultureIgnoreCase) && name.Contains("color", StringComparison.CurrentCultureIgnoreCase), 100, color.Value);
            EmissiveColor(name.Contains("emissive", StringComparison.CurrentCultureIgnoreCase), 80, color.Value);
        }

        foreach (var p in ScalarParameterValues)
        {
            var name = p.Name;
            var v = p.ParameterValue;
            MetallicValue(name.Contains("metallic", StringComparison.CurrentCultureIgnoreCase) && !name.Contains("overwrite", StringComparison.CurrentCultureIgnoreCase), 100, v);
            MetallicValue(name.Contains("metal", StringComparison.CurrentCultureIgnoreCase), 80, v);
            RoughnessValue(name.Contains("roughness", StringComparison.CurrentCultureIgnoreCase) && !name.Contains("min", StringComparison.CurrentCultureIgnoreCase), 100, v);
            SpecularValue(name.Contains("specular", StringComparison.CurrentCultureIgnoreCase), 100, v);
        }

        if (BasePropertyOverrides != null)
        {
            parameters.IsTransparent = BasePropertyOverrides.BlendMode == EBlendMode.BLEND_Translucent;
        }

        // try to get diffuse texture when nothing found
        if (parameters.Diffuse == null && TextureParameterValues.Length == 1)
            parameters.Diffuse = TextureParameterValues[0].ParameterValue.Load<UTexture>();
    }

    public override void GetParams(CMaterialParams2 parameters, EMaterialFormat format)
    {
        if (format != EMaterialFormat.FirstLayer && Parent != null && Parent != this)
            Parent.GetParams(parameters, format);

        parameters.AppendAllProperties(Properties);
        base.GetParams(parameters, format);

        foreach (var textureParameter in TextureParameterValues)
        {
            if (!textureParameter.ParameterValue.TryLoad(out UTexture texture))
                continue;

            if (!parameters.VerifyTexture(textureParameter.Name, texture))
                parameters.VerifyTexture(texture.Name, texture);
        }

        foreach (var vectorParameter in VectorParameterValues)
        {
            if (vectorParameter.ParameterValue is not { } vector)
                continue;
            parameters.Colors[vectorParameter.Name] = vector;
        }

        foreach (var scalarParameter in ScalarParameterValues)
            parameters.Scalars[scalarParameter.Name] = scalarParameter.ParameterValue;
    }

    public override void AppendReferencedTextures(IList<UUnrealMaterial> outTextures, bool onlyRendered)
    {
        if (onlyRendered)
        {
            // default implementation does that
            base.AppendReferencedTextures(outTextures, true);
        }
        else
        {
            foreach (var value in TextureParameterValues)
            {
                var parameterValue = value.ParameterValue.Load<UTexture>();
                if (parameterValue != null && !outTextures.Contains(parameterValue))
                    outTextures.Add(parameterValue);
            }

            if (Parent != null && Parent != this)
                Parent.AppendReferencedTextures(outTextures, onlyRendered);
        }
    }
}
