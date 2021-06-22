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

        public Package(FArchive uasset, FArchive? uexp, Lazy<FArchive?>? ubulk = null, Lazy<FArchive?>? uptnl = null, IFileProvider? provider = null, TypeMappings? mappings = null)
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

            FAssetArchive uexpAr = uexp == null ? uassetAr : new FAssetArchive(uexp, this, Summary.TotalHeaderSize); // allows embedded uexp data

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

            foreach (var export in ExportMap)
            {
                if (ResolvePackageIndex(export.ClassIndex)?.Object?.Value is not UStruct uStruct) continue;
                var obj = ConstructObject(uStruct);
                obj.Name = export.ObjectName.Text;
                obj.Outer = (ResolvePackageIndex(export.OuterIndex) as ResolvedExportObject)?.Object?.Value ?? this;
                obj.Super = ResolvePackageIndex(export.SuperIndex) as ResolvedExportObject;
                obj.Template = ResolvePackageIndex(export.TemplateIndex) as ResolvedExportObject;
                obj.Flags = (EObjectFlags) export.ObjectFlags;
                export.ExportType = obj.GetType();
                export.ExportObject = new Lazy<UObject>(() =>
                {
                    uexpAr.SeekAbsolute(export.RealSerialOffset, SeekOrigin.Begin);
                    var validPos = uexpAr.Position + export.SerialSize;
                    try
                    {
                        obj.Deserialize(uexpAr, validPos);
#if DEBUG
                        if (validPos != uexpAr.Position)
                            Log.Warning("Did not read {0} correctly, {1} bytes remaining", obj.ExportType, validPos - uexpAr.Position);
                        else
                            Log.Debug("Successfully read {0} at {1} with size {2}", obj.ExportType, export.RealSerialOffset, export.SerialSize);
#endif

                        // TODO right place ???
                        obj.PostLoad();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Could not read {0} correctly", obj.ExportType);
                    }

                    return obj;
                });
            }
        }

        public Package(FArchive uasset, FArchive? uexp, FArchive? ubulk = null, FArchive? uptnl = null,
            IFileProvider? provider = null, TypeMappings? mappings = null)
            : this(uasset, uexp, ubulk != null ? new Lazy<FArchive?>(() => ubulk) : null,
                uptnl != null ? new Lazy<FArchive?>(() => uptnl) : null, provider, mappings) { }

        public Package(string name, byte[] uasset, byte[]? uexp, byte[]? ubulk = null, byte[]? uptnl = null, IFileProvider? provider = null)
            : this(new FByteArchive($"{name}.uasset", uasset), uexp != null ? new FByteArchive($"{name}.uexp", uexp) : null,
                ubulk != null ? new FByteArchive($"{name}.ubulk", ubulk) : null,
                uptnl != null ? new FByteArchive($"{name}.uptnl", uptnl) : null, provider) { }

        public override UObject? GetExportOrNull(string name, StringComparison comparisonType = StringComparison.Ordinal)
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
            if (index == null || index.IsNull)
                return null;
            if (index.IsImport && -index.Index - 1 < ImportMap.Length)
                return ResolveImport(index);
            if (index.IsExport && index.Index - 1 < ExportMap.Length)
                return new ResolvedExportObject(index.Index - 1, this);
            return null;
        }

        private ResolvedObject? ResolveImport(FPackageIndex importIndex)
        {
            var import = ImportMap[-importIndex.Index - 1];
            if (import.ClassName.Text == "Class")
            {
                return new ResolvedLoadedObject(-importIndex.Index - 1, new UScriptClass(import.ObjectName.Text));
            }
            var outerMostIndex = importIndex;
            FObjectImport outerMostImport;
            while (true)
            {
                outerMostImport = ImportMap[-outerMostIndex.Index - 1];
                if (outerMostImport.OuterIndex.IsNull)
                    break;
                outerMostIndex = outerMostImport.OuterIndex;
            }

            outerMostImport = ImportMap[-outerMostIndex.Index - 1];
            if (outerMostImport.ObjectName.Text.StartsWith("/Script/"))
            {
                return null; // TODO handle script CDO references
            }

            if (Provider == null)
                return null;
            Package? importPackage = null;
            if (Provider.TryLoadPackage(outerMostImport.ObjectName.Text, out var package))
                importPackage = package as Package;
            if (importPackage == null)
            {
                Log.Error("Missing native package ({0}) for import of {1} in {2}.", outerMostImport.ObjectName, import.ObjectName, Name);
                return null;
            }

            string? outer = null;
            if (outerMostIndex != import.OuterIndex && import.OuterIndex.IsImport)
            {
                var outerImport = ImportMap[-import.OuterIndex.Index - 1];
                outer = ResolveImport(import.OuterIndex)?.GetPathName();
                if (outer == null)
                {
                    Log.Fatal("Missing outer for import of ({0}): {1} in {2} was not found, but the package exists.", Name, outerImport.ObjectName, importPackage.GetFullName());
                    return null;
                }
            }

            for (var i = 0; i < importPackage.ExportMap.Length; i++)
            {
                FObjectExport export = importPackage.ExportMap[i];
                if (export.ObjectName.Text != import.ObjectName.Text)
                    continue;
                var thisOuter = ResolvePackageIndex(export.OuterIndex);
                if (thisOuter?.GetPathName() == outer)
                    return new ResolvedExportObject(i, importPackage);
            }

            Log.Fatal("Missing import of ({0}): {1} in {2} was not found, but the package exists.", Name, import.ObjectName, importPackage.GetFullName());
            return null;
        }

        private class ResolvedExportObject : ResolvedObject
        {
            private readonly FObjectExport _export;

            public ResolvedExportObject(int index, Package package) : base(package, index)
            {
                _export = package.ExportMap[index];
            }

            public override FName Name => _export.ObjectName;
            public override ResolvedObject Outer => Package.ResolvePackageIndex(_export.OuterIndex) ?? new ResolvedLoadedObject(Index, (UObject) Package);
            public override ResolvedObject? Class => Package.ResolvePackageIndex(_export.ClassIndex);
            public override ResolvedObject? Super => Package.ResolvePackageIndex(_export.SuperIndex);
            public override Lazy<UObject> Object => _export.ExportObject;
        }
    }
}