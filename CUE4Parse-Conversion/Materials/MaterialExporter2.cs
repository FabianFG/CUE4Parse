using System.Collections.Generic;
using System.IO;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using Newtonsoft.Json;
using SkiaSharp;

namespace CUE4Parse_Conversion.Materials
{
    public struct MaterialData
    {
        public Dictionary<string, string> Textures;
        public CMaterialParams2 Parameters;
    }

    public class MaterialExporter2 : ExporterBase
    {
        private readonly string _internalFilePath;
        private readonly MaterialData _materialData;

        public MaterialExporter2()
        {
            _internalFilePath = string.Empty;
            _materialData = new MaterialData
            {
                Textures = new Dictionary<string, string>(),
                Parameters = new CMaterialParams2()
            };
        }

        public MaterialExporter2(UUnrealMaterial? unrealMaterial) : this()
        {
            if (unrealMaterial == null) return;
            _internalFilePath = unrealMaterial.Owner?.Name ?? unrealMaterial.Name;

            unrealMaterial.GetParams(_materialData.Parameters, Options.MaterialFormat == EMaterialFormat.AllLayers);
            foreach ((string key, UUnrealMaterial value) in _materialData.Parameters.Textures)
            {
                _materialData.Textures[key] = value.GetPathName();
            }
        }

        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
        {
            label = string.Empty;
            savedFilePath = string.Empty;
            if (!baseDirectory.Exists) return false;

            savedFilePath = FixAndCreatePath(baseDirectory, _internalFilePath, "json");
            File.WriteAllText(savedFilePath, JsonConvert.SerializeObject(_materialData, Formatting.Indented));
            label = Path.GetFileName(savedFilePath);

            foreach (var texture in _materialData.Parameters.Textures.Values)
            {
                if (texture is not UTexture2D t || t.Decode(Options.Platform) is not { } bitmap) continue;

                var texturePath = FixAndCreatePath(baseDirectory, t.Owner?.Name ?? t.Name, "png");
                using var fs = new FileStream(texturePath, FileMode.Create, FileAccess.Write);
                using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = data.AsStream();
                stream.CopyTo(fs);
            }

            return true;
        }

        public override bool TryWriteToZip(out byte[] zipFile)
        {
            throw new System.NotImplementedException();
        }

        public override void AppendToZip()
        {
            throw new System.NotImplementedException();
        }
    }
}
