using System.Collections.Generic;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public class CMaterialParams2
    {
        public const string DefaultDiffuse = "DefaultDiffuse";
        public const string DefaultNormal = "DefaultNormal";

        public bool IsTransparent = false;
        public bool IsNull => Textures.Count == 0 &&
                              Colors.Count == 0 &&
                              Scalars.Count == 0;

        public static readonly string[][] Diffuse = {
            new []{ "Diffuse", "Diffuse A", "Albedo", "Base Color" },
            new []{ "Diffuse_Texture_2" },
            new []{ "Diffuse_Texture_3" },
            new []{ "Diffuse_Texture_4" },
            new []{ "Diffuse_Texture_5" },
            new []{ "Diffuse_Texture_6" },
            new []{ "Diffuse_Texture_7" },
            new []{ DefaultDiffuse }
        };

        public static readonly string[][] Normals = {
            new []{ "Normals", "Normal", "Texture A Normal" },
            new []{ "Normals_Texture_2", "Texture B Normal" },
            new []{ "Normals_Texture_3" },
            new []{ "Normals_Texture_4" },
            new []{ "Normals_Texture_5" },
            new []{ "Normals_Texture_6" },
            new []{ "Normals_Texture_7" },
            new []{ DefaultNormal }
        };

        public static readonly string[][] SpecularMasks = {
            new []{ "SpecularMasks", "MRAE", "MRAS", "MRA", "MRS" },
            new []{ "SpecularMasks_2" },
            new []{ "SpecularMasks_3" },
            new []{ "SpecularMasks_4" },
            new []{ "SpecularMasks_5" },
            new []{ "SpecularMasks_6" },
            new []{ "SpecularMasks_7" },
            new []{ "SpecularMasks_8" }
        };

        public static readonly string[][] Emissive = {
            new []{ "Emissive", "EmissiveTexture" },
            new []{ "L1_Emissive" },
            new []{ "L2_Emissive" },
            new []{ "L3_Emissive" },
            new []{ "L4_Emissive" },
            new []{ "L5_Emissive" },
            new []{ "L6_Emissive" },
            new []{ "L7_Emissive" }
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

                Properties[property.Name.Text] = property.Tag.GenericValue;
            }
        }
    }
}
