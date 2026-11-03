using System.Numerics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Writers.USD;

namespace CUE4Parse_Conversion.Formats.Materials;

/// <summary>
/// TODO: support layers (st, st1, st2, etc)
/// </summary>
public sealed class UsdMaterialFormat(ExportSession session) : IExportFormat
{
    public string DisplayName => "USD Material (.usda)";

    public ExportFile Build(string objectName, PbrMaterialDto source, string packageDirectory = "")
    {
        var matPrim = UsdPrim.Def("Material", objectName);
        var matName = matPrim.Name;

        matPrim.Add(new UsdAttribute("token", "outputs:surface.connect", UsdValue.Path($"/{matName}/PBRShader.outputs:surface")));

        var shader = UsdPrim.Def("Shader", "PBRShader");
        shader.Add(UsdAttribute.Uniform("token", "info:id", UsdValue.Token("UsdPreviewSurface")));
        shader.Add(UsdAttribute.Uniform("int", "inputs:useSpecularWorkflow", UsdValue.Int(0)));
        shader.Add(new UsdAttribute("color3f", "inputs:diffuseColor", UsdValue.Tuple(source.BaseColor.X, source.BaseColor.Y, source.BaseColor.Z)));
        shader.Add(new UsdAttribute("float", "inputs:roughness", UsdValue.Float(source.Roughness)));
        shader.Add(new UsdAttribute("color3f", "inputs:emissiveColor", UsdValue.From(source.Emissive)));

        var diffuse = AttachTexture("Diffuse", source.BaseColorTexture, "sRGB");
        if (diffuse != null)
        {
            diffuse.Add(new UsdAttribute("float4", "inputs:scale", UsdValue.From(source.BaseColor)));
            shader.Add(new UsdAttribute("color3f", "inputs:diffuseColor.connect", UsdValue.Path($"/{matName}/Diffuse.outputs:rgb")));
        }

        if (source.UseOpacity)
        {
            shader.Add(new UsdAttribute("float", "inputs:opacity", UsdValue.Float(source.BaseColor.W)));
            var opacity = AttachTexture("Opacity", source.OpacityTexture, "raw");
            if (opacity != null)
            {
                opacity.Add(new UsdAttribute("float4", "inputs:scale", UsdValue.Tuple(source.BaseColor.W, 1f, 1f, 1f)));
                shader.Add(new UsdAttribute("float", "inputs:opacity.connect", UsdValue.Path($"/{matName}/Opacity.outputs:r")));
            }
            else if (diffuse != null)
            {
                shader.Add(new UsdAttribute("float", "inputs:opacity.connect", UsdValue.Path($"/{matName}/Diffuse.outputs:a")));
            }

            if (source.BlendMode is EBlendMode.BLEND_Masked)
            {
                shader.Add(new UsdAttribute("float", "inputs:opacityThreshold", UsdValue.Float(source.AlphaCutoff)));
            }
        }

        var normal = AttachTexture("Normal", source.NormalTexture, "raw");
        if (normal != null)
        {
            normal.Add(new UsdAttribute("float4", "inputs:bias", UsdValue.Tuple(-1f, -1f, -1f, -1f)));
            normal.Add(new UsdAttribute("float4", "inputs:scale", UsdValue.Tuple(2f, 2f, 2f, 2f)));
            shader.Add(new UsdAttribute("normal3f", "inputs:normal.connect", UsdValue.Path($"/{matName}/Normal.outputs:rgb")));
        }

        const string channels = "rgb";
        var (occlusionChannel, roughnessChannel, metallicChannel) = source.OrmTextureLayout.GetPackedChannels();
        var orm = AttachTexture("ORM", source.OrmTexture, "raw");
        shader.Add(new UsdAttribute("float", "inputs:metallic", UsdValue.Float(source.GetMetallic(orm != null))));
        if (orm != null)
        {
            var scale = Vector4.One;
            scale[roughnessChannel] = source.Roughness;
            scale[metallicChannel] = source.Metallic;
            orm.Add(new UsdAttribute("float4", "inputs:scale", UsdValue.From(scale)));
            shader.Add(new UsdAttribute("float", "inputs:roughness.connect", UsdValue.Path($"/{matName}/ORM.outputs:{channels[roughnessChannel]}")));
            shader.Add(new UsdAttribute("float", "inputs:metallic.connect", UsdValue.Path($"/{matName}/ORM.outputs:{channels[metallicChannel]}")));
        }

        if (AttachTexture("Occlusion", source.OcclusionTexture, "raw") != null)
        {
            shader.Add(new UsdAttribute("float", "inputs:occlusion.connect", UsdValue.Path($"/{matName}/Occlusion.outputs:r")));
        }
        else if (orm != null && occlusionChannel >= 0)
        {
            shader.Add(new UsdAttribute("float", "inputs:occlusion.connect", UsdValue.Path($"/{matName}/ORM.outputs:{channels[occlusionChannel]}")));
        }

        var emissive = AttachTexture("Emissive", source.EmissiveTexture, "sRGB");
        if (emissive != null)
        {
            emissive.Add(new UsdAttribute("float4", "inputs:scale", UsdValue.Tuple(source.Emissive.X, source.Emissive.Y, source.Emissive.Z, 1f)));
            shader.Add(new UsdAttribute("color3f", "inputs:emissiveColor.connect", UsdValue.Path($"/{matName}/Emissive.outputs:rgb")));
        }

        shader.Add(new UsdAttribute("token", "outputs:surface", UsdValue.Declared));

        var primvar = UsdPrim.Def("Shader", "Primvar");
        primvar.Add(UsdAttribute.Uniform("token", "info:id", UsdValue.Token("UsdPrimvarReader_float2")));
        primvar.Add(new UsdAttribute("token", "inputs:varname", UsdValue.Token("st")));
        primvar.Add(new UsdAttribute("float2", "outputs:result", UsdValue.Declared));

        matPrim.Add(shader);
        matPrim.Add(primvar);
        return new ExportFile("usda", new UsdStage(matPrim).SerializeToBinary());

        UsdPrim? AttachTexture(string name, UTexture? texture, string colorSpace)
        {
            if (texture == null)
                return null;

            var path = session.Cache.GetTexturePath(texture);
            if (path == null)
                return null;

            var directory = Path.Combine(session.BaseDirectory.FullName, packageDirectory);
            var relativePath = Path.GetRelativePath(directory, path).Replace('\\', '/');
            var prim = MakeUvTexture(name, relativePath, matName, colorSpace);
            matPrim.Add(prim);
            return prim;
        }
    }

    private static UsdPrim MakeUvTexture(string name, string filePath, string matName, string colorSpace)
    {
        var prim = UsdPrim.Def("Shader", name);
        prim.Add(UsdAttribute.Uniform("token", "info:id", UsdValue.Token("UsdUVTexture")));
        prim.Add(new UsdAttribute("asset", "inputs:file", UsdValue.AssetPath(filePath)));
        prim.Add(new UsdAttribute("float2", "inputs:st.connect", UsdValue.Path($"/{matName}/Primvar.outputs:result")));
        prim.Add(UsdAttribute.Uniform("token", "inputs:sourceColorSpace", UsdValue.Token(colorSpace)));
        prim.Add(new UsdAttribute("float3", "outputs:rgb", UsdValue.Declared));
        prim.Add(new UsdAttribute("float", "outputs:r", UsdValue.Declared));
        prim.Add(new UsdAttribute("float", "outputs:g", UsdValue.Declared));
        prim.Add(new UsdAttribute("float", "outputs:b", UsdValue.Declared));
        prim.Add(new UsdAttribute("float", "outputs:a", UsdValue.Declared));
        return prim;
    }
}
