using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
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
                "ShadedDiffuse", "Diffuse", "DiffuseTexture", "Diffuse A", "Albedo",
                "Base Color", "BaseColor", "Color", "CO", "CO_", "CO_1",
                "Decal_Texture", "PetalDetailMap", "CliffTexture"
            },
            new []{ "Diffuse_Texture_2", "Diffuse B", "CO_2" },
            new []{ "Diffuse_Texture_3", "Diffuse C", "CO_3" },
            new []{ "Diffuse_Texture_4", "Diffuse D", "CO_4" },
            new []{ "Diffuse_Texture_5", "Diffuse E", "CO_5" },
            new []{ "Diffuse_Texture_6", "Diffuse F", "CO_6" },
            new []{ "Diffuse_Texture_7", "Diffuse G", "CO_7" },
            new []{ "Diffuse_Texture_8", "Diffuse H", "CO_8" }
        };

        public static readonly string[][] Normals = {
            new []
            {
                "Normals", "Normal", "NormalTexture", "NormalMap", "NM", "NM_1",
                "Texture A Normal", "CliffNormal"
            },
            new []{ "Normals_Texture_2", "Texture B Normal", "NM_2" },
            new []{ "Normals_Texture_3", "Texture C Normal", "NM_3" },
            new []{ "Normals_Texture_4", "Texture D Normal", "NM_4" },
            new []{ "Normals_Texture_5", "Texture E Normal", "NM_5" },
            new []{ "Normals_Texture_6", "Texture F Normal", "NM_6" },
            new []{ "Normals_Texture_7", "Texture G Normal", "NM_7" },
            new []{ "Normals_Texture_8", "Texture H Normal", "NM_8" }
        };

        public static readonly string[][] SpecularMasks = {
            new []
            {
                "SpecularMasks", "Specular", "PackedTexture", "SpecMap",
                "ORM", "MRAE", "MRAS", "MRA", "MRS", "LP", "LP_1",
                "Cliff Spec Texture"
            },
            new []{ "SpecularMasks_2", "LP_2" },
            new []{ "SpecularMasks_3" },
            new []{ "SpecularMasks_4" },
            new []{ "SpecularMasks_5" },
            new []{ "SpecularMasks_6" },
            new []{ "SpecularMasks_7" },
            new []{ "SpecularMasks_8" }
        };

        public static readonly string[][] Emissive = {
            new []
            {
                "Emissive", "EmissiveTexture", "EmissiveColor", "EmissiveMask",
                "SkinFX_Mask"
            },
            new []{ "L1_Emissive" },
            new []{ "L2_Emissive" },
            new []{ "L3_Emissive" },
            new []{ "L4_Emissive" },
            new []{ "L5_Emissive" },
            new []{ "L6_Emissive" },
            new []{ "L7_Emissive" }
        };

        public static readonly string[][] DiffuseColors = {
            new []
            {
                "ColorMult", "Color_mul", "Base Color", "Color"
            },
            new []{ "" },
            new []{ "" },
            new []{ "" },
            new []{ "" },
            new []{ "" },
            new []{ "" },
            new []{ "" }
        };

        public static readonly string[][] EmissiveColors = {
            new []
            {
                "Emissive", "EmissiveColor", "Emissive Color"
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

        public bool TryGetTexture2d(out UTexture2D? texture, params string[] names)
        {
            foreach (string name in names)
            {
                if (Textures.TryGetValue(name, out var unrealMaterial) && unrealMaterial is UTexture2D texture2d)
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
