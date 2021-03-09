using System;
using System.IO;
using System.Linq;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Assets
{
    public sealed class Package : AbstractUePackage
    {
        public const uint PackageMagic = 0x9E2A83C1u;

        public override FPackageFileSummary Summary { get; }
        public override FNameEntrySerialized[] NameMap { get; }
        public FObjectImport[] ImportMap { get; }
        public FObjectExport[] ExportMap { get; }
        public override Lazy<UObject>[] ExportsLazy => ExportMap.Select(it => it.ExportObject).ToArray();

        public Package(FArchive uasset, FArchive uexp, Lazy<FArchive?>? ubulk = null, Lazy<FArchive?>? uptnl = null, IFileProvider? provider = null, TypeMappings? mappings = null)
            : base(uasset.Name.SubstringBeforeLast(".uasset"), provider, mappings)
        {
            var uassetAr = new FAssetArchive(uasset, this);
            Summary = new FPackageFileSummary(uassetAr);
            if (Summary.Tag != PackageMagic)
            {
                throw new ParserException(uassetAr, $"Invalid uasset magic: {Summary.Tag} != {PackageMagic}");
            }

            uassetAr.Seek(Summary.NameOffset, SeekOrigin.Begin);
            NameMap = new FNameEntrySerialized[Summary.NameCount];
            uassetAr.ReadArray(NameMap, () => new FNameEntrySerialized(uassetAr));

            uassetAr.Seek(Summary.ImportOffset, SeekOrigin.Begin);
            ImportMap = new FObjectImport[Summary.ImportCount];
            uassetAr.ReadArray(ImportMap, () => new FObjectImport(uassetAr));

            uassetAr.Seek(Summary.ExportOffset, SeekOrigin.Begin);
            ExportMap = new FObjectExport[Summary.ExportCount]; // we need this to get its final size in some case
            uassetAr.ReadArray(ExportMap, () => new FObjectExport(uassetAr));

            var uexpAr = new FAssetArchive(uexp, this, Summary.TotalHeaderSize);
            if (ubulk != null)
            {
                //var offset = (int) (Summary.TotalHeaderSize + ExportMap.Sum(export => export.SerialSize));
                var offset = Summary.BulkDataStartOffset;
                uexpAr.AddPayload(PayloadType.UBULK, offset, ubulk);
            }

            if (uptnl != null)
            {
                var offset = Summary.BulkDataStartOffset;
                uexpAr.AddPayload(PayloadType.UPTNL, offset, uptnl);
            }

            foreach (var it in ExportMap)
            {
                var exportType = (it.ClassName == string.Empty || it.ClassName == "None") && !(this is IoPackage) ? uexpAr.ReadFName().Text : it.ClassName;
                var export = ConstructObject(null);
                it.ExportType = export.GetType();
                it.ExportObject = new Lazy<UObject>(() =>
                {
                    var validPos = uexpAr.Position + it.SerialSize;
                    uexpAr.SeekAbsolute(it.RealSerialOffset, SeekOrigin.Begin);
                    try
                    {
                        export.Deserialize(uexpAr, validPos);
#if DEBUG
                        if (validPos != uexpAr.Position)
                            Log.Warning("Did not read {0} correctly, {1} bytes remaining", exportType, validPos - uexpAr.Position);
                        else
                            Log.Debug("Successfully read {0} at {1} with size {2}", exportType, it.RealSerialOffset, it.SerialSize);
#endif
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Could not read {0} correctly", exportType);
                    }

                    return export;
                });
            }
        }

        public Package(FArchive uasset, FArchive uexp, FArchive? ubulk = null, FArchive? uptnl = null,
            IFileProvider? provider = null, TypeMappings? mappings = null)
            : this(uasset, uexp, ubulk != null ? new Lazy<FArchive?>(() => ubulk) : null,
                uptnl != null ? new Lazy<FArchive?>(() => uptnl) : null, provider, mappings) { }

        public Package(string name, byte[] uasset, byte[] uexp, byte[]? ubulk = null, byte[]? uptnl = null, IFileProvider? provider = null)
            : this(new FByteArchive($"{name}.uasset", uasset), new FByteArchive($"{name}.uexp", uexp),
                ubulk != null ? new FByteArchive($"{name}.ubulk", ubulk) : null,
                uptnl != null ? new FByteArchive($"{name}.uptnl", uptnl) : null, provider) { }

        public override UExport? GetExportOrNull(string name, StringComparison comparisonType = StringComparison.Ordinal)
        {
            try
            {
                return ExportMap
                    .FirstOrDefault(it => it.ObjectName.Text.Equals(name, comparisonType))?.ExportObject
                    .Value;
            }
            catch (Exception e)
            {
                Log.Debug(e, "Failed to get export object");
                return null;
            }
        }

        public override ResolvedObject? ResolvePackageIndex(FPackageIndex? index)
        {
            if (index == null || index.IsNull) return null;
            if (index.IsExport) return new ResolvedExportObject(ExportMap[index.Index - 1], this);
            var import = ImportMap[-index.Index - 1];
            if (import.ClassName.Text == "Class")
            {
                return null; // TODO
            }
            //The needed export is located in another asset, try to load it
            if (!import.OuterIndex.IsImport) return null;
            return null; // TODO VERY HUGE
        }

        private class ResolvedExportObject : ResolvedObject
        {
            public FObjectExport Export;

            public ResolvedExportObject(FObjectExport export, Package package) : base(package)
            {
                Export = export;
            }

            public override FName Name => Export.ObjectName;
            public override ResolvedObject? Outer => Package.ResolvePackageIndex(Export.OuterIndex);
            public override ResolvedObject? Super => Package.ResolvePackageIndex(Export.SuperIndex);
            public override Lazy<UObject> Object => Export.ExportObject;
        }
    }
}