using System.Collections.Generic;
using System.IO;
using CUE4Parse_Conversion.Materials;

namespace CUE4Parse_Conversion.Meshes
{
    public class Mesh : ExporterBase
    {
        public readonly string FileName;
        public readonly byte[] FileData;
        public readonly List<MaterialExporter> Materials;

        public Mesh(string fileName, byte[] fileData, List<MaterialExporter> materials)
        {
            FileName = fileName;
            FileData = fileData;
            Materials = materials;
        }

        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string savedFileName)
        {
            savedFileName = string.Empty;
            if (!baseDirectory.Exists || FileData.Length <= 0) return false;

            foreach (var material in Materials)
            {
                material.TryWriteToDir(baseDirectory, out _);
            }
            
            var filePath = FixAndCreatePath(baseDirectory, FileName);
            File.WriteAllBytes(filePath, FileData);
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