using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
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

        public bool HasTopDiffuse => HasTopTexture(Diffuse[0]);
        public bool HasTopNormals => HasTopTexture(Normals[0]);
        public bool HasTopSpecularMasks => HasTopTexture(SpecularMasks[0]);
        public bool HasTopEmissive => HasTopTexture(Emissive[0]);

        public bool IsTransparent = false;
        public bool IsNull => Textures.Count == 0;

        /// <summary>
        /// SWITCH TO REGEX ONCE WE HAVE A GOOD OVERVIEW OF TEXTURE NAMES
        /// AND POSSIBLY RE-USE THE REGEX FOR BOTH UMaterial & UMaterialInstanceConstant
        /// </summary>

        public static readonly string[][] Diffuse = {
            new []
            {
                "Trunk_BaseColor", "ShadedDiffuse", "Background Diffuse", "Diffuse", "Diffuse_1", "DiffuseTexture", "Diffuse A", "Albedo",
                "Base Color Texture", "BaseColorTexture", "Base Color", "BaseColor", "BaseColorA", "BC", "Color", "CO", "CO_", "CO_1", "Base_CO",
                "Decal_Texture", "PetalDetailMap", "CliffTexture"
            },
            new []{ "Background Diffuse 2", "Diffuse_Texture_2", "Diffuse B", "BaseColorB", "CO_2" },
            new []{ "Background Diffuse 3", "Diffuse_Texture_3", "Diffuse C", "BaseColorC", "CO_3" },
            new []{ "Background Diffuse 4", "Diffuse_Texture_4", "Diffuse D", "BaseColorD", "CO_4" },
            new []{ "Background Diffuse 5", "Diffuse_Texture_5", "Diffuse E", "BaseColorE", "CO_5" },
            new []{ "Background Diffuse 6", "Diffuse_Texture_6", "Diffuse F", "BaseColorF", "CO_6" },
            new []{ "Background Diffuse 7", "Diffuse_Texture_7", "Diffuse G", "BaseColorG", "CO_7" },
            new []{ "Background Diffuse 8", "Diffuse_Texture_8", "Diffuse H", "BaseColorH", "CO_8" }
        };

        public static readonly string[][] Normals = {
            new []
            {
                "Trunk_Normal", "Normals", "Normal", "NormalA", "NormalTexture", "Normal Texture", "NormalMap", "NM", "NM_1", "Base_NM",
                "Texture A Normal", "CliffNormal"
            },
            new []{ "Normals_Texture_2", "Texture B Normal", "NormalB", "NM_2" },
            new []{ "Normals_Texture_3", "Texture C Normal", "NormalC", "NM_3" },
            new []{ "Normals_Texture_4", "Texture D Normal", "NormalD", "NM_4" },
            new []{ "Normals_Texture_5", "Texture E Normal", "NormalE", "NM_5" },
            new []{ "Normals_Texture_6", "Texture F Normal", "NormalF", "NM_6" },
            new []{ "Normals_Texture_7", "Texture G Normal", "NormalG", "NM_7" },
            new []{ "Normals_Texture_8", "Texture H Normal", "NormalH", "NM_8" }
        };

        public static readonly string[][] SpecularMasks = {
            new []
            {
                "Trunk_Specular", "SpecularMasks", "Specular", "SpecMap",
                "MG", "ORM", "MRAE", "MRAS", "MRA", "MRS", "LP", "LP_1", "Base_LP",
                "Cliff Spec Texture"
            },
            new []{ "SpecularMasks_2", "LP_2" },
            new []{ "SpecularMasks_3", "LP_3" },
            new []{ "SpecularMasks_4", "LP_4" },
            new []{ "SpecularMasks_5", "LP_5" },
            new []{ "SpecularMasks_6", "LP_6" },
            new []{ "SpecularMasks_7", "LP_7" },
            new []{ "SpecularMasks_8", "LP_8" }
        };

        public static readonly string[][] Emissive = {
            new []
            {
                "Emissive", "EmissiveTexture", "EmissiveColorTexture", "EmissiveColor", "EmissiveMask",
                "EmmisiveColor_A"
            },
            new []{ "L1_Emissive", "EmmisiveColor_B" },
            new []{ "L2_Emissive", "EmmisiveColor_C" },
            new []{ "L3_Emissive", "EmmisiveColor_D" },
            new []{ "L4_Emissive", "EmmisiveColor_E" },
            new []{ "L5_Emissive", "EmmisiveColor_F" },
            new []{ "L6_Emissive", "EmmisiveColor_G" },
            new []{ "L7_Emissive", "EmmisiveColor_H" }
        };

        public static readonly string[][] DiffuseColors = {
            new []
            {
                "ColorMult", "Color_mul", "Base Color", "BaseColor", "Color", "tex1_CO",
                "ColorA"
            },
            new []{ "tex2_CO", "ColorB" },
            new []{ "tex3_CO", "ColorC" },
            new []{ "tex4_CO", "ColorD" },
            new []{ "tex5_CO", "ColorE" },
            new []{ "tex6_CO", "ColorF" },
            new []{ "tex7_CO", "ColorG" },
            new []{ "tex8_CO", "ColorH" }
        };

        public static readonly string[][] EmissiveColors = {
            new []
            {
                "Emissive", "Emissive Color", "EmissiveColor"
            },
            new []{ "Emissive1" },
            new []{ "Emissive2" },
            new []{ "Emissive3" },
            new []{ "Emissive4" },
            new []{ "Emissive5" },
            new []{ "Emissive6" },
            new []{ "Emissive7" }
        };

        public readonly Dictionary<string, UUnrealMaterial> Textures = new ();
        public readonly Dictionary<string, FLinearColor> Colors = new ();
        public readonly Dictionary<string, float> Scalars = new ();
        public readonly Dictionary<string, bool> Switchs = new ();
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

        public bool TryGetFirstTexture2d(out UTexture2D? texture)
        {
            if (Textures.First() is { Value: UTexture2D texture2D })
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

        public bool TryGetTexture2d(out UTexture2D? texture, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (Textures.TryGetValue(names[i], out var unrealMaterial) && unrealMaterial is UTexture2D texture2d)
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

        public void AppendAllProperties(IList<FPropertyTag> properties)
        {
            foreach (var property in properties)
            {
                if (property.Name.Text is "Parent" or "TextureParameterValues" or "VectorParameterValues" or "ScalarParameterValues")
                    continue;

                Properties[property.Name.Text] = property.Tag?.GenericValue;
            }
        }

        private bool HasTopTexture(params string[] names)
        {
            foreach (string name in names)
                if (Textures.ContainsKey(name))
                    return true;

            return false;
        }
    }
}
