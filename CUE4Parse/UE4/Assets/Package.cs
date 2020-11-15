using System.IO;
using System.Linq;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Assets
{
    public sealed class Package : AbstractUePackage
    {
        public const uint PackageMagic = 0x9E2A83C1u;

        public override FPackageFileSummary Summary { get; }
        public override FNameEntrySerialized[] NameMap { get; }
        public override FObjectImport[] ImportMap { get; }
        public override FObjectExport[] ExportMap { get; }

        public Package(FArchive uasset, FArchive uexp, FArchive? ubulk = null, FArchive? uptnl = null, IFileProvider? provider = null, TypeMappings? mappings = null)
            : base(uasset.Name.SubstringBeforeLast(".uasset"), provider, mappings)
        {
            var uassetAr = new FAssetArchive(uasset, this);
            Summary = new FPackageFileSummary(uassetAr);
            if (Summary.Tag != PackageMagic)
            {
                throw new ParserException(uassetAr, $"Invalid uasset magic: {Summary.Tag} != {PackageMagic}");
            }

            uassetAr.Seek(Summary.NameOffset, SeekOrigin.Begin);
            NameMap = uassetAr.ReadArray(Summary.NameCount, () => new FNameEntrySerialized(uassetAr));

            uassetAr.Seek(Summary.ImportOffset, SeekOrigin.Begin);
            ImportMap = uassetAr.ReadArray(Summary.ImportCount, () => new FObjectImport(uassetAr));

            uassetAr.Seek(Summary.ExportOffset, SeekOrigin.Begin);
            ExportMap = uassetAr.ReadArray(Summary.ExportCount, () => new FObjectExport(uassetAr));

            var uexpAr = new FAssetArchive(uexp, this, Summary.TotalHeaderSize);
            if (ubulk != null)
            {
                //var offset = (int) (Summary.TotalHeaderSize + ExportMap.Sum(export => export.SerialSize));
                var offset = Summary.BulkDataStartOffset;
                var ubulkAr = new FAssetArchive(ubulk, this, offset);
                uexpAr.AddPayload(PayloadType.UBULK, ubulkAr);
            }
            if (uptnl != null)
            {
                // TODO Not sure whether that's even needed
                var offset = (int) (Summary.TotalHeaderSize + ExportMap.Sum(export => export.SerialSize));
                var uptnlAr = new FAssetArchive(uptnl, this, offset);
                uexpAr.AddPayload(PayloadType.UPTNL, uptnlAr);
            }

            ProcessExportMap(uexpAr);
        }

        public Package(string name, byte[] uasset, byte[] uexp, byte[]? ubulk = null, byte[]? uptnl = null, IFileProvider? provider = null)
            : this(new FByteArchive($"{name}.uasset", uasset), new FByteArchive($"{name}.uexp", uexp),
                ubulk != null ? new FByteArchive($"{name}.ubulk", ubulk) : null,
                uptnl != null ? new FByteArchive($"{name}.uptnl", uptnl) : null, provider)
        {
        }
    }
}