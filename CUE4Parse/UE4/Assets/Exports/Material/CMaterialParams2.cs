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

        public static readonly string[] Diffuse = { "Diffuse", "Diffuse_Texture_2", "Diffuse_Texture_3", "Diffuse_Texture_4" };
        public static readonly string[] Normals = { "Normals", "Normals_Texture_2", "Normals_Texture_3", "Normals_Texture_4" };
        public static readonly string[] SpecularMasks = { "SpecularMasks", "SpecularMasks_2", "SpecularMasks_3", "SpecularMasks_4" };
        public static readonly string[] Emissive = { "EmissiveTexture", "L1_Emissive", "L2_Emissive", "Emissive Texture 2" }; // idk tbh

        public readonly Dictionary<string, UUnrealMaterial> Textures = new ();
        public readonly Dictionary<string, FLinearColor> Colors = new ();
        public readonly Dictionary<string, float> Scalars = new ();
        public readonly Dictionary<string, object?> Properties = new ();

        public IEnumerable<UUnrealMaterial?> GetDiffuseTextures() => GetTexturesOrNull(Diffuse);
        public IEnumerable<UUnrealMaterial?> GetNormalsTextures() => GetTexturesOrNull(Normals);
        public IEnumerable<UUnrealMaterial?> GetSpecularMasksTextures() => GetTexturesOrNull(SpecularMasks);
        public IEnumerable<UUnrealMaterial?> GetEmissiveTextures() => GetTexturesOrNull(Emissive);

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

        public bool TryGetTexture2d(out UUnrealMaterial? texture, params string[] names)
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
