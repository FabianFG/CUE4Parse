using System.Collections.Generic;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public class CMaterialParams2
    {
        public const string DefaultDiffuse = "DefaultDiffuse";
        public const string DefaultNormal = "DefaultNormal";

        // UV       1 2 3 4
        // TEXTURE  1 3 4 2
        public static readonly string[] Diffuse = { "Diffuse", "Diffuse_Texture_3", "Diffuse_Texture_4", "Diffuse_Texture_2" };
        public static readonly string[] Normals = { "Normals", "Normals_Texture_3", "Normals_Texture_4", "Normals_Texture_2" };
        public static readonly string[] SpecularMasks = { "SpecularMasks", "SpecularMasks_3", "SpecularMasks_4", "SpecularMasks_2" };

        public readonly Dictionary<string, UUnrealMaterial> Textures = new ();
        public readonly Dictionary<string, FLinearColor> Colors = new ();
        public readonly Dictionary<string, float> Scalars = new ();
        public readonly Dictionary<string, object?> Properties = new ();

        public IEnumerable<UUnrealMaterial> GetDiffuseTextures() => GetTextures(Diffuse);
        public IEnumerable<UUnrealMaterial> GetNormalsTextures() => GetTextures(Normals);
        public IEnumerable<UUnrealMaterial> GetSpecularMasksTextures() => GetTextures(SpecularMasks);

        public IEnumerable<UUnrealMaterial> GetTextures(IEnumerable<string> names)
        {
            foreach(string name in names)
                if (Textures.TryGetValue(name, out var y))
                    yield return y;
        }

        public IEnumerable<UUnrealMaterial> GetTexturesByRegex(Regex regex)
        {
            foreach ((string key, UUnrealMaterial value) in Textures)
                if (regex.IsMatch(key))
                    yield return value;
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
