using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Textures;
using SkiaSharp;

namespace CUE4Parse_Conversion.Materials
{
    public class MaterialExporter : ExporterBase
    {
        private readonly string _internalFilePath;
        private readonly string _fileData;
        private readonly IDictionary<string, SKImage?> _textures;
        private readonly MaterialExporter? _parentData;

        public MaterialExporter()
        {
            _internalFilePath = string.Empty;
            _fileData = string.Empty;
            _textures = new Dictionary<string, SKImage?>();
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
                
                var bNearest = false;
                if (t.TryGetValue(out FName trigger, "LODGroup", "Filter") && !trigger.IsNone)
                    bNearest = trigger.Text.EndsWith("TEXTUREGROUP_Pixels2D", StringComparison.OrdinalIgnoreCase) ||
                               trigger.Text.EndsWith("TF_Nearest", StringComparison.OrdinalIgnoreCase);
                
                _textures[t.Owner?.Name ?? t.Name] = t.Decode(bNearest);
            }

            if (unrealMaterial is UMaterialInstanceConstant {Parent: { }} material)
                _parentData = new MaterialExporter(material.Parent);
        }

        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string savedFileName)
        {
            savedFileName = string.Empty;
            if (!baseDirectory.Exists || string.IsNullOrEmpty(_fileData)) return false;

            var filePath = FixAndCreatePath(baseDirectory, _internalFilePath, "mat");
            File.WriteAllText(filePath, _fileData);
            savedFileName = Path.GetFileName(filePath);

            foreach (var kvp in _textures)
            {
                if (kvp.Value == null) continue;
                
                var texturePath = FixAndCreatePath(baseDirectory, kvp.Key, "png");
                using var stream = new FileStream(texturePath, FileMode.Create, FileAccess.Write);
                kvp.Value.Encode().AsStream().CopyTo(stream);
            }

            if (_parentData != null)
                _parentData.TryWriteToDir(baseDirectory, out _);

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