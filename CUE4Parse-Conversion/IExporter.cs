using System;
using System.IO;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.Utils;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;

namespace CUE4Parse_Conversion
{
    public struct ExporterOptions
    {
        public ELodFormat LodFormat;
        public EMeshFormat MeshFormat;
        public EMaterialFormat MaterialFormat;
        public ETextureFormat TextureFormat;
        public ETexturePlatform Platform;
        public ESocketFormat SocketFormat;
        public bool ExportMorphTargets;

        public ExporterOptions()
        {
            LodFormat = ELodFormat.FirstLod;
            MeshFormat = EMeshFormat.ActorX;
            MaterialFormat = EMaterialFormat.FirstLayer;
            TextureFormat = ETextureFormat.Png;
            Platform = ETexturePlatform.DesktopMobile;
            SocketFormat = ESocketFormat.Bone;
            ExportMorphTargets = true;
        }
    }

    public interface IExporter
    {
        public bool TryWriteToDir(DirectoryInfo directoryInfo, out string label, out string savedFileName);
        public bool TryWriteToZip(out byte[] zipFile);
        public void AppendToZip();
    }

    public abstract class ExporterBase : IExporter
    {
        protected readonly string PackagePath;
        protected readonly string ExportName;
        public ExporterOptions Options;

        protected ExporterBase()
        {
            PackagePath = string.Empty;
            ExportName = string.Empty;
        }

        protected ExporterBase(UObject export)
        {
            var p = export.GetPathName();
            PackagePath = p.SubstringBeforeLast('.');
            ExportName = p.SubstringAfterLast('.');
        }

        public abstract bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath);
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

        public Exporter(UObject export, ExporterOptions options)
        {
            Options = options;
            _exporterBase = export switch
            {
                UAnimSequence animSequence => new AnimExporter(animSequence),
                UMaterialInterface material => new MaterialExporter2(material),
                USkeletalMesh skeletalMesh => new MeshExporter(skeletalMesh),
                USkeleton skeleton => new MeshExporter(skeleton),
                UStaticMesh staticMesh => new MeshExporter(staticMesh),
                _ => throw new ArgumentOutOfRangeException(nameof(export), export, null)
            };
        }

        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath) =>
            _exporterBase.TryWriteToDir(baseDirectory, out label, out savedFilePath);

        public override bool TryWriteToZip(out byte[] zipFile) => _exporterBase.TryWriteToZip(out zipFile);

        public override void AppendToZip() => _exporterBase.AppendToZip();
    }
}
