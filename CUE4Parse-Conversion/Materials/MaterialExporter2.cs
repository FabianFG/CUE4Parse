using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using static CUE4Parse_Conversion.Textures.TextureEncoder;


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

        public MaterialExporter2(ExporterOptions options)
        {
            Options = options;
            _internalFilePath = string.Empty;
            _materialData = new MaterialData
            {
                Textures = new Dictionary<string, string>(),
                Parameters = new CMaterialParams2()
            };
        }

        public MaterialExporter2(UUnrealMaterial? unrealMaterial, ExporterOptions options) : this(options)
        {
            if (unrealMaterial == null) return;
            _internalFilePath = (unrealMaterial.Owner?.Provider?.FixPath(unrealMaterial.Owner.Name) ??
                                 unrealMaterial.Name).SubstringBeforeLast('.');

            unrealMaterial.GetParams(_materialData.Parameters, Options.MaterialFormat);
            foreach ((string key, UUnrealMaterial value) in _materialData.Parameters.Textures)
            {
                _materialData.Textures[key] = value.GetPathName();
            }
        }

        private readonly object _texture = new ();
        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
        {
            label = string.Empty;
            savedFilePath = string.Empty;

            savedFilePath = FixAndCreatePath(baseDirectory, _internalFilePath, "json");
            File.WriteAllTextAsync(savedFilePath, JsonConvert.SerializeObject(_materialData, Formatting.Indented));
            label = Path.GetFileName(savedFilePath);

            Parallel.ForEach(_materialData.Parameters.Textures.Values, texture =>
            {
                if (texture is not UTexture2D t || t.Decode(Options.Platform) is not { } bitmap) return;

                lock (_texture)
                {
                    var imageData = bitmap.Encode(Options.TextureFormat, out var ext);
                    var texturePath = FixAndCreatePath(baseDirectory,(t.Owner?.Provider?.FixPath(t.Owner.Name) ?? t.Name).SubstringBeforeLast('.'), ext);
                    using var fs = new FileStream(texturePath, FileMode.Create, FileAccess.Write);
                    fs.Write(imageData, 0, imageData.Length);
                }
            });

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
