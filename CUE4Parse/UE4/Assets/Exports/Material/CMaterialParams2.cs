using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public struct TextureMapping
{
    public int UVSet;
    public int Index;
}

public class CMaterialParams2
{
    public const string FallbackDiffuse = "PM_Diffuse";
    public const string FallbackNormals = "PM_Normals";
    public const string FallbackSpecularMasks = "PM_SpecularMasks";
    public const string FallbackEmissive = "PM_Emissive";

    public const string RegexDiffuse = ".*(?:Diff|_Tex|_?Albedo|_?Base_?Color).*|(?:_D|_DIF|_DM|_C|_CM)$";
    public const string RegexNormals = "^NO_|.*Norm.*|(?:_N|_NM|_NRM)$";
    public const string RegexSpecularMasks = "^SP_|.*(?:Specu|_S_|MR|(?<!no)RM).*|(?:_S|_LP|_PAK)$";
    public const string RegexEmissive = ".*Emiss.*|(?:_E|_EM)$";

    public bool HasTopDiffuse => HasTopTexture(Diffuse[0]);
    public bool HasTopNormals => HasTopTexture(Normals[0]);
    public bool HasTopSpecularMasks => HasTopTexture(SpecularMasks[0]);
    public bool HasTopEmissive => HasTopTexture(Emissive[0]);

    public EBlendMode BlendMode = EBlendMode.BLEND_Opaque;
    public EMaterialShadingModel ShadingModel = EMaterialShadingModel.MSM_Unlit;

    public bool IsTranslucent => BlendMode == EBlendMode.BLEND_Translucent;
    public bool IsNull => Textures.Count == 0;

    /// <summary>
    /// SWITCH TO REGEX ONCE WE HAVE A GOOD OVERVIEW OF TEXTURE NAMES
    /// AND POSSIBLY RE-USE THE REGEX FOR BOTH UMaterial & UMaterialInstanceConstant
    /// </summary>

    public static readonly string[][] Diffuse =
    [
        [
            "Trunk_BaseColor", "ShadedDiffuse", "LitDiffuse",
            "Background Diffuse", "BG Diffuse Texture", "Diffuse", "Diffuse_1", "DiffuseTexture", "DiffuseMap", "Diffuse A", "Diffuse A Map", "Diffuse Top", "Diffuse Side", "Base Diffuse", "Diffuse Base", "Diffuse Base Map", "Diffuse Color Map", "DiffuseLayer1",
            "Albedo", "ALB", "TextureAlbedo",
            "Base Color Texture", "BaseColorTexture", "Base_Color", "Base Color", "BaseColor", "Base Texture Color", "BaseColorA", "BC", "Color", "CO", "CO_", "CO_1", "Base_CO",
            "Tex", "Tex_Color", "TexColor", "Tex_BaseColor", "AlbedMap", "Tex_Colormap",
            "Decal_Texture", "PetalDetailMap", "CliffTexture", "M1_T_BC", "Skin Diffuse"
        ],
        ["Background Diffuse 2", "Diffuse_Texture_2", "DiffuseLayer2", "Diffuse B", "Diffuse B Map", "BaseColorB", "CO_2", "M2_T_BC"],
        ["Background Diffuse 3", "Diffuse_Texture_3", "DiffuseLayer3", "Diffuse C", "Diffuse C Map", "BaseColorC", "CO_3", "M3_T_BC"],
        ["Background Diffuse 4", "Diffuse_Texture_4", "DiffuseLayer4", "Diffuse D", "Diffuse D Map", "BaseColorD", "CO_4", "M4_T_BC"],
        ["Background Diffuse 5", "Diffuse_Texture_5", "DiffuseLayer5", "Diffuse E", "Diffuse E Map", "BaseColorE", "CO_5", "M5_T_BC"],
        ["Background Diffuse 6", "Diffuse_Texture_6", "DiffuseLayer6", "Diffuse F", "Diffuse F Map", "BaseColorF", "CO_6", "M6_T_BC"],
        ["Background Diffuse 7", "Diffuse_Texture_7", "DiffuseLayer7", "Diffuse G", "Diffuse G Map", "BaseColorG", "CO_7", "M7_T_BC"],
        ["Background Diffuse 8", "Diffuse_Texture_8", "DiffuseLayer8", "Diffuse H", "Diffuse H Map", "BaseColorH", "CO_8", "M8_T_BC"]
    ];

    public static readonly string[][] Normals =
    [
        [
            "Trunk_Normal",
            "Normals", "Normal", "NormalA", "NormalTexture", "Normal Texture", "NormalMap", "Normal A Map", "T_Normal", "Normals Top", "Normals Side", "Fallback Normal",
            "Base_Normal", "Base Normal", "Normal Base", "TextureNormal", "Tex_BakedNormal", "TexNor", "BakedNormalMap", "Base Texture Normal", "Normal Base Map",
            "NM", "NM_1", "Base_NM", "NRM", "T_NRM", "M1_T_NRM", "Base NRM", "NRM Base",
            "Texture A Normal", "CliffNormal", "Skin Normal"
        ],
        ["Normals_Texture_2", "Texture B Normal", "NormalB", "Normal B Map", "NM_2", "M2_T_NRM"],
        ["Normals_Texture_3", "Texture C Normal", "NormalC", "Normal C Map", "NM_3", "M3_T_NRM"],
        ["Normals_Texture_4", "Texture D Normal", "NormalD", "Normal D Map", "NM_4", "M4_T_NRM"],
        ["Normals_Texture_5", "Texture E Normal", "NormalE", "Normal E Map", "NM_5", "M5_T_NRM"],
        ["Normals_Texture_6", "Texture F Normal", "NormalF", "Normal F Map", "NM_6", "M6_T_NRM"],
        ["Normals_Texture_7", "Texture G Normal", "NormalG", "Normal G Map", "NM_7", "M7_T_NRM"],
        ["Normals_Texture_8", "Texture H Normal", "NormalH", "Normal H Map", "NM_8", "M8_T_NRM"]
    ];

    public static readonly string[][] SpecularMasks =
    [
        [
            "Trunk_Specular", "PackedTexture",
            "SpecularMasks", "Specular", "SpecMap", "T_Specular", "Specular Top", "Specular Side",
            "MG", "ORM", "MRAE", "MRAS", "MRAO", "MRA", "MRA A", "MRS", "LP", "LP_1", "Base_LP",
            "TextureRMA", "Tex_MultiMask", "Tex_Multi", "TexMRC", "TexMRA", "TexRCN", "MultiMaskMap", "MRO Map", "MROA Map",
            "Base_SRO", "Base Texture RMAO", "Skin SRXO", "SRXO_Mask", "SRXO", "SROA", "SR", "SRO Map", "SRM",
            "Pack", "PAK", "T_PAK", "M1_T_PAK",
            "Cliff Spec Texture", "PhysicalMap", "KizokMap"
        ],
        ["SpecularMasks_2", "MRA B", "LP_2", "M2_T_PAK"],
        ["SpecularMasks_3", "MRA C", "LP_3", "M3_T_PAK"],
        ["SpecularMasks_4", "MRA D", "LP_4", "M4_T_PAK"],
        ["SpecularMasks_5", "MRA E", "LP_5", "M5_T_PAK"],
        ["SpecularMasks_6", "MRA F", "LP_6", "M6_T_PAK"],
        ["SpecularMasks_7", "MRA G", "LP_7", "M7_T_PAK"],
        ["SpecularMasks_8", "MRA H", "LP_8", "M8_T_PAK"]
    ];

    public static readonly string[][] Emissive =
    [
        [
            "Emissive", "EmissiveTexture", "EmissiveColorTexture", "EmissiveColor", "EmissiveMask",
            "EmmisiveColor_A", "TextureEmissive", "TexEm"
        ],
        ["L1_Emissive", "EmmisiveColor_B"],
        ["L2_Emissive", "EmmisiveColor_C"],
        ["L3_Emissive", "EmmisiveColor_D"],
        ["L4_Emissive", "EmmisiveColor_E"],
        ["L5_Emissive", "EmmisiveColor_F"],
        ["L6_Emissive", "EmmisiveColor_G"],
        ["L7_Emissive", "EmmisiveColor_H"]
    ];

    public static readonly string[][] DiffuseColors =
    [
        [
            "ColorMult", "Color_mul", "Base Color", "BaseColor", "Color", "DiffuseColor", "tex1_CO",
            "ColorA", "ALB", "AlbedoColor"
        ],
        ["tex2_CO", "ColorB"],
        ["tex3_CO", "ColorC"],
        ["tex4_CO", "ColorD"],
        ["tex5_CO", "ColorE"],
        ["tex6_CO", "ColorF"],
        ["tex7_CO", "ColorG"],
        ["tex8_CO", "ColorH"]
    ];

    public static readonly string[][] EmissiveColors =
    [
        [
            "Emissive", "Emissive Color", "EmissiveColor", "EMI", "EmColor", "Color"
        ],
        ["Emissive1", "Color01"],
        ["Emissive2", "Color02"],
        ["Emissive3", "Color03"],
        ["Emissive4", "Color04"],
        ["Emissive5", "Color05"],
        ["Emissive6", "Color06"],
        ["Emissive7", "Color07"]
    ];

    [JsonIgnore]
    public readonly Dictionary<string, UUnrealMaterial> Textures = new ();
    public readonly Dictionary<string, FLinearColor> Colors = new ();
    public readonly Dictionary<string, float> Scalars = new ();
    public readonly Dictionary<string, bool> Switches = new ();
    public readonly Dictionary<string, object?> Properties = new ();

    public IEnumerable<UUnrealMaterial> GetTextures(IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            if (Textures.TryGetValue(name, out var y))
                yield return y;
        }
    }

    public IEnumerable<UUnrealMaterial?> GetTexturesOrNull(IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            if (Textures.TryGetValue(name, out var y))
                yield return y;
            else yield return null;
        }
    }

    public IEnumerable<UUnrealMaterial> GetTexturesByRegex(Regex regex)
    {
        foreach ((string key, UUnrealMaterial value) in Textures)
            if (regex.IsMatch(key))
                yield return value;
    }

    public bool TryGetFirstTexture2d(out UTexture? texture)
    {
        if (Textures.FirstOrDefault() is { Value: UTexture texture2D })
        {
            texture = texture2D;
            return true;
        }

        texture = null;
        return false;
    }

    /// <summary>
    /// find matching textures between <paramref name="names"/> and <see cref="Textures"/>
    /// </summary>
    /// <param name="numTexCoords"></param>
    /// <param name="names"></param>
    /// <returns>
    /// the uv set and index in <paramref name="names"/> of all textures found limited to <paramref name="numTexCoords"/>
    /// <br/>
    /// if used with <paramref name="names"/> it basically gives the textures' key in <see cref="Textures"/>
    /// </returns>
    public TextureMapping[] GetTextureMapping(int numTexCoords, params string[][] names)
    {
        var uvset = 0;
        var index = 0;
        var mapping = new TextureMapping[numTexCoords];
        for (int i = 0; i < mapping.Length; i++)
        {
            for (; uvset < names.Length; uvset++)
            {
                var found = false;
                for (; index < names[uvset].Length; index++)
                {
                    if (Textures.ContainsKey(names[uvset][index]))
                    {
                        mapping[i] = new TextureMapping { UVSet = uvset, Index = index };
                        found = true;
                        break;
                    }
                }

                index++;
                if (found) break;
                index = 0;
            }
        }
        return mapping;
    }

    public bool TryGetTexture2d(out UTexture? texture, params string[] names)
    {
        foreach (var name in names)
        {
            if (Textures.TryGetValue(name, out var unrealMaterial) && unrealMaterial is UTexture texture2d)
            {
                texture = texture2d;
                return true;
            }
        }

        texture = null;
        return false;
    }

    public bool TryGetLinearColor(out FLinearColor linearColor, params string[] names)
    {
        foreach (string name in names)
        {
            if (Colors.TryGetValue(name, out linearColor))
            {
                return true;
            }
        }

        linearColor = default;
        return false;
    }

    public bool TryGetScalar(out float scalar, params string[] names)
    {
        foreach (string name in names)
        {
            if (Scalars.TryGetValue(name, out scalar))
            {
                return true;
            }
        }

        scalar = 0f;
        return false;
    }

    public bool TryGetSwitch(out bool stitch, params string[] names)
    {
        foreach (string name in names)
        {
            if (Switches.TryGetValue(name, out stitch))
            {
                return true;
            }
        }

        stitch = false;
        return false;
    }

    public void AppendAllProperties(IList<FPropertyTag> properties)
    {
        foreach (var property in properties)
        {
            if (property.Name.Text is "Parent" or
                "TextureParameterValues" or
                "VectorParameterValues" or
                "ScalarParameterValues" or
                "StaticParameters" or
                "StaticParametersRuntime" or
                "CachedExpressionData" or
                "CachedReferencedTextures" or
                "TextureStreamingData" or
                "BlendMode" or
                "ShadingModel")
                continue;

            Properties[property.Name.Text] = property.Tag?.GenericValue;
        }
    }

    public bool VerifyTexture(string name, UTexture texture, bool appendToDictionary = true, EMaterialSamplerType samplerType = EMaterialSamplerType.SAMPLERTYPE_Color)
    {
        var fallback = "";
        if (Regex.IsMatch(name, RegexDiffuse, RegexOptions.IgnoreCase))
            fallback = FallbackDiffuse;
        else if (samplerType == EMaterialSamplerType.SAMPLERTYPE_Normal ||
                 Regex.IsMatch(name, RegexNormals, RegexOptions.IgnoreCase))
            fallback = FallbackNormals;
        else if (Regex.IsMatch(name, RegexSpecularMasks, RegexOptions.IgnoreCase))
            fallback = FallbackSpecularMasks;
        else if (Regex.IsMatch(name, RegexEmissive, RegexOptions.IgnoreCase))
            fallback = FallbackEmissive;

        var ret = !string.IsNullOrEmpty(fallback);
        if (ret) Textures[fallback] = texture;
        if (appendToDictionary) Textures[name] = texture;
        return ret;
    }

    private bool HasTopTexture(params string[] names)
    {
        foreach (string name in names)
            if (Textures.ContainsKey(name))
                return true;

        return false;
    }
}
