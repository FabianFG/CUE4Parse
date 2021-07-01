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
            
            foreach (var it in ExportMap)
            {
                if (ResolvePackageIndex(it.ClassIndex)?.Object?.Value is not UStruct uStruct) continue;
                var export = ConstructObject(uStruct);
                export.Name = it.ObjectName.Text;
                export.Outer = (ResolvePackageIndex(it.OuterIndex) as ResolvedExportObject)?.Object?.Value ?? this;
                export.Super = ResolvePackageIndex(it.SuperIndex) as ResolvedExportObject;
                export.Template = ResolvePackageIndex(it.TemplateIndex) as ResolvedExportObject;
                export.Flags |= (EObjectFlags) it.ObjectFlags; // We give loaded objects the RF_WasLoaded flag in ConstructObject, so don't remove it again in here 
                it.ExportType = export.GetType();
                it.ExportObject = new Lazy<UObject>(() =>
                {
                    uexpAr.SeekAbsolute(it.RealSerialOffset, SeekOrigin.Begin);
                    var validPos = uexpAr.Position + it.SerialSize;
                    try
                    {
                        export.Deserialize(uexpAr, validPos);
#if DEBUG
                        if (validPos != uexpAr.Position)
                            Log.Warning("Did not read {0} correctly, {1} bytes remaining", export.ExportType, validPos - uexpAr.Position);
                        else
                            Log.Debug("Successfully read {0} at {1} with size {2}", export.ExportType, it.RealSerialOffset, it.SerialSize);
#endif
                        
                        // TODO right place ???
                        export.Flags |= EObjectFlags.RF_LoadCompleted;
                        export.PostLoad();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Could not read {0} correctly", export.ExportType);
                    }

                    return export;
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
                return new ResolvedScriptObject(-index.Index - 1, this);
            if (index.IsExport && index.Index - 1 < ExportMap.Length)
                return new ResolvedExportObject(index.Index - 1, this);
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
            public override ResolvedObject? Outer => Package.ResolvePackageIndex(_export.OuterIndex);
            public override ResolvedObject? Super => Package.ResolvePackageIndex(_export.SuperIndex);
            public override Lazy<UObject>? Object => Super?.Object ?? _export.ExportObject ?? null;
        }
        
        private class ResolvedScriptObject : ResolvedObject
        {
            private readonly FObjectImport _import;

            public ResolvedScriptObject(int index, Package package) : base(package, index)
            {
                _import = package.ImportMap[index];
            }

            public override FName Name => _import.ObjectName;
            public override ResolvedObject? Outer => Package.ResolvePackageIndex(_import.OuterIndex);
            public override ResolvedObject Class => new ResolvedLoadedObject(Index, new UScriptClass(_import.ClassName.Text));
            public override Lazy<UObject> Object => new(() => new UScriptClass(Name.Text));
        }
    }
}