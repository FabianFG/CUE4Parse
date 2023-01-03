using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;
using SkiaSharp;

namespace CUE4Parse_Conversion.Materials
{
    public class MaterialExporter : ExporterBase
    {
        private readonly string _internalFilePath;
        private readonly string _fileData;
        private readonly MaterialExporter? _parentData;
        private readonly IDictionary<string, SKBitmap?> _textures;

        public MaterialExporter()
        {
            _internalFilePath = string.Empty;
            _fileData = string.Empty;
            _textures = new Dictionary<string, SKBitmap?>();
            _parentData = null;
        }

        public MaterialExporter(UUnrealMaterial? unrealMaterial) : this()
        {
            if (unrealMaterial == null) return;
            _internalFilePath = unrealMaterial.Owner?.Name ?? unrealMaterial.Name;

            var allTextures = new List<UUnrealMaterial>();
            unrealMaterial.AppendReferencedTextures(allTextures, false);

            var parameters = new CMaterialParams();
            unrealMaterial.GetParams(parameters);
            if ((parameters.IsNull || parameters.Diffuse == unrealMaterial) && allTextures.Count == 0)
                return;

            var sb = new StringBuilder();
            var toExport = new List<UUnrealMaterial>();
            void Proc(string name, UUnrealMaterial? arg)
            {
                if (arg == null) return;
                sb.AppendLine($"{name}={arg.Name}");
                toExport.Add(arg);
            }

            Proc("Diffuse", parameters.Diffuse);
            Proc("Normal", parameters.Normal);
            Proc("Specular", parameters.Specular);
            Proc("SpecPower", parameters.SpecPower);
            Proc("Opacity", parameters.Opacity);
            Proc("Emissive", parameters.Emissive);
            Proc("Cube", parameters.Cube);
            Proc("Mask", parameters.Mask);
            Proc("Misc", parameters.Misc);

            // Export other textures
            var numOtherTextures = 0;
            foreach (var texture in allTextures)
            {
                if (toExport.Contains(texture)) continue;
                Proc($"Other[{numOtherTextures++}]", texture);
            }

            _fileData = sb.ToString().Trim();

            foreach (var texture in toExport)
            {
                if (texture == unrealMaterial || texture is not UTexture2D t) continue;
                _textures[t.Owner?.Name ?? t.Name] = t.Decode(Options.Platform);
            }

            if (unrealMaterial is UMaterialInstanceConstant {Parent: { }} material)
                _parentData = new MaterialExporter(material.Parent) { Options = Options };
        }

        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
        {
            label = string.Empty;
            savedFilePath = string.Empty;
            if (!baseDirectory.Exists || string.IsNullOrEmpty(_fileData)) return false;

            savedFilePath = FixAndCreatePath(baseDirectory, _internalFilePath, "mat");
            File.WriteAllText(savedFilePath, _fileData);
            label = Path.GetFileName(savedFilePath);

            foreach ((string? name, SKBitmap? bitmap) in _textures)
            {
                if (bitmap == null) continue;

                var texturePath = FixAndCreatePath(baseDirectory, name, "png");
                using var fs = new FileStream(texturePath, FileMode.Create, FileAccess.Write);
                using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = data.AsStream();
                stream.CopyTo(fs);
            }

            if (_parentData != null)
                _parentData.TryWriteToDir(baseDirectory, out _, out _);

            return true;
        }

        public override bool TryWriteToZip(out byte[] zipFile)
        {
            throw new NotImplementedException();
        }

        public override void AppendToZip()
        {
            throw new NotImplementedException();
        }
    }
}
