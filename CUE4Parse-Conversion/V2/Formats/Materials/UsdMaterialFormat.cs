using CUE4Parse_Conversion.USD;
using CUE4Parse.UE4.Assets.Exports.Material;

namespace CUE4Parse_Conversion.V2.Formats.Materials;

public sealed class UsdMaterialFormat : IMaterialExportFormat
{
    public string DisplayName => "USD Material (.usda)";

    public ExportFile Build(string objectName, CMaterialParams2 parameters, string packageDirectory = "")
    {
        var matPrim = UsdPrim.Def("Material", objectName);
        var matName = matPrim.Name;

        matPrim.Add(new UsdAttribute("token", "outputs:surface.connect", UsdValue.Path($"/{matName}/PBRShader.outputs:surface")));

        var shader = UsdPrim.Def("Shader", "PBRShader");
        shader.Add(UsdAttribute.Uniform("token", "info:id", UsdValue.Token("UsdPreviewSurface")));
        shader.Add(UsdAttribute.Uniform("int", "inputs:useSpecularWorkflow", UsdValue.Int(0)));

        var hasDiffuseTexture = TryResolveTexture(parameters, [..CMaterialParams2.Diffuse[0], CMaterialParams2.FallbackDiffuse], packageDirectory, out var diffusePath);
        var hasDiffuseTint = parameters.TryGetLinearColor(out var diffuseColor, CMaterialParams2.DiffuseColors[0]);

        if (hasDiffuseTexture)
        {
            var uvTex = MakeUvTexture("Diffuse", diffusePath, matName);
            if (hasDiffuseTint)
            {
                uvTex.Add(new UsdAttribute("float4", "inputs:scale", UsdValue.Tuple(diffuseColor.R, diffuseColor.G, diffuseColor.B, 1f)));
            }

            matPrim.Add(uvTex);
            shader.Add(new UsdAttribute("color3f", "inputs:diffuseColor.connect", UsdValue.Path($"/{matName}/Diffuse.outputs:rgb")));

            if (parameters.BlendMode != EBlendMode.BLEND_Opaque)
            {
                shader.Add(new UsdAttribute("float", "inputs:opacity.connect", UsdValue.Path($"/{matName}/Diffuse.outputs:a")));
                if (parameters.BlendMode == EBlendMode.BLEND_Masked)
                {
                    shader.Add(new UsdAttribute("float", "inputs:opacityThreshold", UsdValue.Float(0.333f)));
                }
            }
        }
        else if (hasDiffuseTint)
        {
            shader.Add(new UsdAttribute("color3f", "inputs:diffuseColor", UsdValue.Tuple(diffuseColor.R, diffuseColor.G, diffuseColor.B)));
        }

        if (TryResolveTexture(parameters, [..CMaterialParams2.Normals[0], CMaterialParams2.FallbackNormals], packageDirectory, out var normalPath))
        {
            var uvTex = MakeUvTexture("Normal", normalPath, matName);
            uvTex.Add(new UsdAttribute("float4", "inputs:bias", UsdValue.Tuple(-1f, -1f, -1f, -1f)));
            uvTex.Add(new UsdAttribute("float4", "inputs:scale", UsdValue.Tuple(2f, 2f, 2f, 2f)));
            uvTex.Add(UsdAttribute.Uniform("token", "inputs:sourceColorSpace", UsdValue.Token("raw")));
            matPrim.Add(uvTex);

            shader.Add(new UsdAttribute("normal3f", "inputs:normal.connect", UsdValue.Path($"/{matName}/Normal.outputs:rgb")));
        }

        if (TryResolveTexture(parameters, [..CMaterialParams2.SpecularMasks[0], CMaterialParams2.FallbackSpecularMasks], packageDirectory, out var specPath))
        {
            var uvTex = MakeUvTexture("Specular", specPath, matName, ormOutputs: true);
            uvTex.Add(UsdAttribute.Uniform("token", "inputs:sourceColorSpace", UsdValue.Token("raw")));
            matPrim.Add(uvTex);

            shader.Add(new UsdAttribute("float", "inputs:roughness.connect", UsdValue.Path($"/{matName}/Specular.outputs:b")));
            shader.Add(new UsdAttribute("float", "inputs:metallic.connect", UsdValue.Path($"/{matName}/Specular.outputs:g")));
        }
        else
        {
            if (parameters.TryGetScalar(out var roughMax, "RoughnessMax", "SpecRoughnessMax", "Roughness"))
                shader.Add(new UsdAttribute("float", "inputs:roughness", UsdValue.Float(roughMax)));
            if (parameters.TryGetScalar(out var metallic, "Metallic", "MetallicScale"))
                shader.Add(new UsdAttribute("float", "inputs:metallic", UsdValue.Float(metallic)));
        }

        if (TryResolveTexture(parameters, CMaterialParams2.Emissive[0], packageDirectory, out var emissivePath))
        {
            var uvTex = MakeUvTexture("Emissive", emissivePath, matName);
            if (parameters.TryGetLinearColor(out var emissiveColor, CMaterialParams2.EmissiveColors[0]))
            {
                uvTex.Add(new UsdAttribute("float4", "inputs:scale", UsdValue.Tuple(emissiveColor.R, emissiveColor.G, emissiveColor.B)));
            }

            matPrim.Add(uvTex);
            shader.Add(new UsdAttribute("color3f", "inputs:emissiveColor.connect", UsdValue.Path($"/{matName}/Emissive.outputs:rgb")));
        }

        if (!hasDiffuseTexture && parameters.IsTranslucent)
        {
            shader.Add(new UsdAttribute("float", "inputs:opacity", UsdValue.Float(0.5f)));
        }

        shader.Add(new UsdAttribute("token", "outputs:surface", UsdValue.Declared));

        var primvar = UsdPrim.Def("Shader", "Primvar");
        primvar.Add(UsdAttribute.Uniform("token", "info:id", UsdValue.Token("UsdPrimvarReader_float2")));
        primvar.Add(new UsdAttribute("token", "inputs:varname", UsdValue.Token("st")));
        primvar.Add(new UsdAttribute("float2", "outputs:result", UsdValue.Declared));

        matPrim.Add(shader);
        matPrim.Add(primvar);

        return new ExportFile("usda", new UsdStage(matPrim).SerializeToBinary());
    }

    private bool TryResolveTexture(CMaterialParams2 parameters, string[] names, string packageDirectory, out string path)
    {
        if (parameters.TryGetTexture2d(out var texture, names))
        {
            path = ExporterBase2.Resolve(texture, packageDirectory, "png"); // TODO: not only PNG
            return true;
        }
        path = string.Empty;
        return false;
    }

    private UsdPrim MakeUvTexture(string name, string filePath, string matName, bool ormOutputs = false)
    {
        var prim = UsdPrim.Def("Shader", name);
        prim.Add(UsdAttribute.Uniform("token", "info:id", UsdValue.Token("UsdUVTexture")));
        prim.Add(new UsdAttribute("asset", "inputs:file", UsdValue.AssetPath(filePath)));
        prim.Add(new UsdAttribute("float2", "inputs:st.connect", UsdValue.Path($"/{matName}/Primvar.outputs:result")));

        if (ormOutputs)
        {
            prim.Add(new UsdAttribute("float", "outputs:r", UsdValue.Declared));
            prim.Add(new UsdAttribute("float", "outputs:g", UsdValue.Declared));
            prim.Add(new UsdAttribute("float", "outputs:b", UsdValue.Declared));
        }
        else
        {
            prim.Add(new UsdAttribute("float3", "outputs:rgb", UsdValue.Declared));
            prim.Add(new UsdAttribute("float", "outputs:a", UsdValue.Declared));
        }

        return prim;
    }
}
