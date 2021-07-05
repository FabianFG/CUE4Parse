using System.Collections.Generic;
using System.IO;
using CUE4Parse_Conversion.Materials;

namespace CUE4Parse_Conversion.Meshes
{
    public class StaticMeshExport
    {
        private readonly string _fileName;
        private readonly byte[] _pskxData;
        private readonly List<MaterialExporter> _materials;

        public StaticMeshExport(string fileName, byte[] pskxData, List<MaterialExporter> materials)
        {
            _fileName = fileName;
            _pskxData = pskxData;
            _materials = materials;
        }
        
        public bool TryWriteTo(string mshDirectory, out string fileName)
        {
            fileName = _fileName;
            if (_pskxData.Length <= 0) return false;

            File.WriteAllBytes(Path.Combine(mshDirectory, _fileName), _pskxData);
            return true;
        }
    }
}