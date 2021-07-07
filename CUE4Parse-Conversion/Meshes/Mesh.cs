using System.Collections.Generic;
using System.IO;
using CUE4Parse_Conversion.Materials;

namespace CUE4Parse_Conversion.Meshes
{
    public class Mesh : ExporterBase
    {
        private readonly string _internalFilePath;
        private readonly byte[] _fileData;
        private readonly List<MaterialExporter> _materials;

        public Mesh(string fileName, byte[] fileData, List<MaterialExporter> materials)
        {
            _internalFilePath = fileName;
            _fileData = fileData;
            _materials = materials;
        }

        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string savedFileName)
        {
            savedFileName = string.Empty;
            if (!baseDirectory.Exists || _fileData.Length <= 0) return false;

            foreach (var material in _materials)
            {
                material.TryWriteToDir(baseDirectory, out _);
            }
            
            var filePath = FixAndCreatePath(baseDirectory, _internalFilePath);
            File.WriteAllBytes(filePath, _fileData);
            savedFileName = Path.GetFileName(filePath);
            return File.Exists(filePath);
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