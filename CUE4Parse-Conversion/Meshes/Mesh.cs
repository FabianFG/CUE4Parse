using System.Collections.Generic;
using System.IO;
using CUE4Parse_Conversion.Materials;

namespace CUE4Parse_Conversion.Meshes
{
    public class Mesh : IExporter
    {
        private readonly string _fileName;
        private readonly byte[] _fileData;
        private readonly List<MaterialExporter> _materials;

        public Mesh(string fileName, byte[] fileData, List<MaterialExporter> materials)
        {
            _fileName = fileName;
            _fileData = fileData;
            _materials = materials;
        }

        public bool TryWriteToDir(DirectoryInfo directoryInfo, out string savedFileName)
        {
            savedFileName = _fileName;
            if (_fileData.Length <= 0) return false;

            var filePath = Path.Combine(directoryInfo.FullName, _fileName);
            File.WriteAllBytes(filePath, _fileData);
            return File.Exists(filePath);
        }

        public bool TryWriteToZip(out byte[] zipFile)
        {
            throw new System.NotImplementedException();
        }

        public void AppendToZip()
        {
            throw new System.NotImplementedException();
        }
    }
}