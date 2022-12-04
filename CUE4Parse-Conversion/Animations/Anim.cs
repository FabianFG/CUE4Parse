using System;
using System.IO;

namespace CUE4Parse_Conversion.Animations
{
    public class Anim : ExporterBase
    {
        public readonly string FileName;
        public readonly byte[] FileData;

        public Anim(string fileName, byte[] fileData)
        {
            FileName = fileName;
            FileData = fileData;
        }

        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
        {
            label = string.Empty;
            savedFilePath = string.Empty;
            if (!baseDirectory.Exists || FileData.Length <= 0) return false;

            savedFilePath = FixAndCreatePath(baseDirectory, FileName);
            File.WriteAllBytes(savedFilePath, FileData);
            label = Path.GetFileName(savedFilePath);
            return File.Exists(savedFilePath);
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
