using System;
using System.IO;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.Utils;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes;

namespace CUE4Parse_Conversion
{
    public interface IExporter
    {
        public bool TryWriteToDir(DirectoryInfo directoryInfo, out string savedFileName);
        public bool TryWriteToZip(out byte[] zipFile);
        public void AppendToZip();
    }

    public abstract class ExporterBase : IExporter
    {
        public abstract bool TryWriteToDir(DirectoryInfo baseDirectory, out string savedFileName);
        public abstract bool TryWriteToZip(out byte[] zipFile);
        public abstract void AppendToZip();
        
        protected string FixAndCreatePath(DirectoryInfo baseDirectory, string fullPath, string? ext = null)
        {
            if (fullPath.StartsWith("/")) fullPath = fullPath[1..];
            var ret = Path.Combine(baseDirectory.FullName, fullPath) + (ext != null ? $".{ext.ToLower()}" : "");
            Directory.CreateDirectory(ret.Replace('\\', '/').SubstringBeforeLast('/'));
            return ret;
        }
    }

    public class Exporter : ExporterBase
    {
        private readonly ExporterBase _exporterBase;
        
        public Exporter(UObject export)
        {
            _exporterBase = export switch
            {
                UMaterialInterface material => new MaterialExporter(material, false),
                UStaticMesh staticMesh => new MeshExporter(staticMesh),
                USkeletalMesh skeletalMesh => new MeshExporter(skeletalMesh),
                _ => throw new ArgumentOutOfRangeException(nameof(export), export, null)
            };
        }

        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string savedFileName) =>
            _exporterBase.TryWriteToDir(baseDirectory, out savedFileName);
        
        public override bool TryWriteToZip(out byte[] zipFile) => _exporterBase.TryWriteToZip(out zipFile);

        public override void AppendToZip() => _exporterBase.AppendToZip();
    }
}