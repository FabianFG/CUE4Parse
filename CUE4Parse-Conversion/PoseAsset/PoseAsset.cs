using System;
using System.IO;
using System.Threading.Tasks;

namespace CUE4Parse_Conversion.PoseAsset;

public class PoseAsset : ExporterBase
{
    public readonly string FileName;
    public readonly byte[] FileData;

    public PoseAsset(string fileName, byte[] fileData)
    {
        FileName = fileName;
        FileData = fileData;
    }

    public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
    {
        label = string.Empty;
        savedFilePath = string.Empty;
        if (FileData.Length <= 0) return false;

        savedFilePath = FixAndCreatePath(baseDirectory, FileName);
        File.WriteAllBytesAsync(savedFilePath, FileData);
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