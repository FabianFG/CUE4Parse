using System;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Assets
{
    public sealed class IoPackage : AbstractUePackage
    {
        public readonly FPackageSummary IoSummary;
        public readonly IoGlobalData GlobalData;

        public override FPackageFileSummary Summary { get; }
        public override FNameEntrySerialized[] NameMap { get; }
        public readonly FPackageObjectIndex[] ImportMap;
        public readonly FExportMapEntry[] ExportMap;

        public readonly Lazy<IoPackage?[]> ImportedPackages;
        public override Lazy<UObject>[] ExportsLazy { get; }

        public IoPackage(
            FArchive uasset, IoGlobalData globalData,
            Lazy<FArchive?>? ubulk = null, Lazy<FArchive?>? uptnl = null,
            IFileProvider? provider = null, TypeMappings? mappings = null) : base(uasset.Name.SubstringBeforeLast(".uasset"), provider, mappings)
        {
            if (provider == null)
                throw new ParserException("Cannot load I/O store package without a file provider. This is needed to link the package imports.");

            GlobalData = globalData;
            var uassetAr = new FAssetArchive(uasset, this);

            // Summary
            IoSummary = uassetAr.Read<FPackageSummary>();
            Summary = new FPackageFileSummary
            {
                PackageFlags = IoSummary.PackageFlags,
                TotalHeaderSize = IoSummary.GraphDataOffset + IoSummary.GraphDataSize,
                NameCount = IoSummary.NameMapHashesSize / sizeof(ulong) - 1,
                ExportCount = (IoSummary.ExportBundlesOffset - IoSummary.ExportMapOffset) / Unsafe.SizeOf<FExportMapEntry>(),
                ImportCount = (IoSummary.ExportMapOffset - IoSummary.ImportMapOffset) / FPackageObjectIndex.Size
            };

            // Name map
            uassetAr.Position = IoSummary.NameMapNamesOffset;
            NameMap = FNameEntrySerialized.LoadNameBatch(uassetAr, Summary.NameCount);
            Name = CreateFNameFromMappedName(IoSummary.Name).Text;

            // Import map
            uassetAr.Position = IoSummary.ImportMapOffset;
            ImportMap = uasset.ReadArray<FPackageObjectIndex>(Summary.ImportCount);

            // Export map
            uassetAr.Position = IoSummary.ExportMapOffset;
            ExportMap = uasset.ReadArray<FExportMapEntry>(Summary.ExportCount);
            ExportsLazy = new Lazy<UObject>[Summary.ExportCount];

            // Export bundles
            uassetAr.Position = IoSummary.ExportBundlesOffset;
            LoadExportBundles(uassetAr, out var exportBundleHeaders, out var exportBundleEntries);

            // Graph data
            uassetAr.Position = IoSummary.GraphDataOffset;
            var importedPackageIds = LoadGraphData(uassetAr);

            // Preload dependencies
            ImportedPackages = new Lazy<IoPackage?[]>(() =>
            {
                var packages = new IoPackage?[importedPackageIds.Length];
                for (int i = 0; i < importedPackageIds.Length; i++)
                {
                    provider.TryLoadPackage(importedPackageIds[i], out packages[i]);
                }
                return packages;
            });

            // Attach ubulk and uptnl
            if (ubulk != null) uassetAr.AddPayload(PayloadType.UBULK, Summary.BulkDataStartOffset, ubulk);
            if (uptnl != null) uassetAr.AddPayload(PayloadType.UPTNL, Summary.BulkDataStartOffset, uptnl);

            // Populate lazy exports
            var allExportDataOffset = IoSummary.GraphDataOffset + IoSummary.GraphDataSize;
            var currentExportDataOffset = allExportDataOffset;
            foreach (var exportBundle in exportBundleHeaders)
            {
                for (var i = 0u; i < exportBundle.EntryCount; i++)
                {
                    var entry = exportBundleEntries[exportBundle.FirstEntryIndex + i];
                    if (entry.CommandType == EExportCommandType.ExportCommandType_Serialize)
                    {
                        var localExportIndex = entry.LocalExportIndex;
                        var export = ExportMap[localExportIndex];
                        var localExportDataOffset = currentExportDataOffset;
                        ExportsLazy[localExportIndex] = new Lazy<UObject>(() =>
                        {
                            // Create
                            var objectName = CreateFNameFromMappedName(export.ObjectName);
                            var obj = ConstructObject(ResolveObjectIndex(export.ClassIndex)?.Object?.Value as UStruct);
                            obj.Name = objectName.Text;
                            obj.Outer = (ResolveObjectIndex(export.OuterIndex) as ResolvedExportObject)?.ExportObject.Value ?? this;
                            obj.Super = ResolveObjectIndex(export.SuperIndex) as ResolvedExportObject;
                            obj.Template = ResolveObjectIndex(export.TemplateIndex) as ResolvedExportObject;
                            obj.Flags = (EObjectFlags) export.ObjectFlags;
                            var exportType = obj.ExportType;

                            // Serialize
                            uassetAr.AbsoluteOffset = (int) export.CookedSerialOffset - localExportDataOffset;
                            uassetAr.Seek(localExportDataOffset, SeekOrigin.Begin);
                            var validPos = uassetAr.Position + (long) export.CookedSerialSize;
                            try
                            {
                                obj.Deserialize(uassetAr, validPos);
#if DEBUG
                                if (validPos != uassetAr.Position)
                                    Log.Warning("Did not read {0} correctly, {1} bytes remaining", exportType, validPos - uassetAr.Position);
                                else
                                    Log.Debug("Successfully read {0} at {1} with size {2}", exportType, localExportDataOffset, export.CookedSerialSize);
#endif
                                // TODO right place ???
                                obj.PostLoad();
                            }
                            catch (Exception e)
                            {
                                Log.Error(e, "Could not read {0} correctly", exportType);
                            }
                            return obj;
                        });
                        currentExportDataOffset += (int) export.CookedSerialSize;
                    }
                }
            }

            Summary.BulkDataStartOffset = currentExportDataOffset;
        }

        public IoPackage(FArchive uasset, IoGlobalData globalData, FArchive? ubulk = null, FArchive? uptnl = null, IFileProvider? provider = null, TypeMappings? mappings = null)
            : this(uasset, globalData, ubulk != null ? new Lazy<FArchive?>(() => ubulk) : null, uptnl != null ? new Lazy<FArchive?>(() => uptnl) : null, provider, mappings)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FName CreateFNameFromMappedName(FMappedName mappedName) =>
            new(mappedName, mappedName.IsGlobal ? GlobalData.GlobalNameMap : NameMap);

        private void LoadExportBundles(FArchive reader, out FExportBundleHeader[] bundleHeadersArray, out FExportBundleEntry[] bundleEntriesArray)
        {
            var bundleHeadersBytes = reader.ReadBytes(IoSummary.GraphDataOffset - IoSummary.ExportBundlesOffset);

            unsafe
            {
                fixed (byte* bundleHeadersRaw = bundleHeadersBytes)
                {
                    var bundleHeaders = (FExportBundleHeader*) bundleHeadersRaw;
                    var remainingBundleEntryCount = (IoSummary.GraphDataOffset - IoSummary.ExportBundlesOffset) / sizeof(FExportBundleEntry);
                    var foundBundlesCount = 0;
                    var currentBundleHeader = bundleHeaders;
                    while (foundBundlesCount < remainingBundleEntryCount)
                    {
                        // This location is occupied by header, so it is not a bundle entry
                        remainingBundleEntryCount--;
                        foundBundlesCount += (int) currentBundleHeader->EntryCount;
                        currentBundleHeader++;
                    }

                    if (foundBundlesCount != remainingBundleEntryCount)
                        throw new ParserException(reader, $"FoundBundlesCount {foundBundlesCount} != RemainingBundleEntryCount {remainingBundleEntryCount}");

                    // Load export bundles into arrays
                    bundleHeadersArray = new FExportBundleHeader[currentBundleHeader - bundleHeaders];
                    fixed (FExportBundleHeader* bundleHeadersPtr = bundleHeadersArray)
                    {
                        Unsafe.CopyBlockUnaligned(bundleHeadersPtr, bundleHeaders, (uint) (bundleHeadersArray.Length * sizeof(FExportBundleHeader)));
                    }

                    bundleEntriesArray = new FExportBundleEntry[foundBundlesCount];
                    fixed (FExportBundleEntry* bundleEntriesPtr = bundleEntriesArray)
                    {
                        Unsafe.CopyBlockUnaligned(bundleEntriesPtr, currentBundleHeader, (uint) (foundBundlesCount * sizeof(FExportBundleEntry)));
                    }
                }
            }
        }

        private FPackageId[] LoadGraphData(FAssetArchive Ar)
        {
            var packageCount = Ar.Read<int>();
            if (packageCount == 0) return Array.Empty<FPackageId>();

            var packageIds = new FPackageId[packageCount];
            for (var packageIndex = 0; packageIndex < packageCount; packageIndex++)
            {
                var packageId = Ar.Read<FPackageId>();
                var bundleCount = Ar.Read<int>();
                Ar.Position += bundleCount * (sizeof(int) + sizeof(int)); // Skip FArcs
                packageIds[packageIndex] = packageId;
            }

            return packageIds;
        }

        public override UObject? GetExportOrNull(string name, StringComparison comparisonType = StringComparison.Ordinal)
        {
            for (var i = 0; i < ExportMap.Length; i++)
            {
                var export = ExportMap[i];
                if (CreateFNameFromMappedName(export.ObjectName).Text.Equals(name, comparisonType))
                {
                    return ExportsLazy[i].Value;
                }
            }

            return null;
        }

        public override ResolvedObject? ResolvePackageIndex(FPackageIndex? index)
        {
            if (index == null || index.IsNull)
                return null;
            if (index.IsImport && -index.Index - 1 < ImportMap.Length)
                return ResolveObjectIndex(ImportMap[-index.Index - 1]);
            if (index.IsExport && index.Index - 1 < ExportMap.Length)
                return new ResolvedExportObject(index.Index - 1, this);
            return null;
        }

        public ResolvedObject? ResolveObjectIndex(FPackageObjectIndex index)
        {
            if (index.IsExport)
            {
                return new ResolvedExportObject((int) index.AsExport, this);
            }

            if (index.IsScriptImport)
            {
                var scriptObjectEntry = GlobalData.FindScriptEntryName(index);
                return scriptObjectEntry != "None" ? new ResolvedScriptObject(scriptObjectEntry, this) : null;
            }

            if (index.IsPackageImport)
            {
                foreach (var pkg in ImportedPackages.Value)
                {
                    if (pkg == null) continue;
                    for (int exportIndex = 0; exportIndex < pkg.ExportMap.Length; ++exportIndex)
                        if (pkg.ExportMap[exportIndex].GlobalImportIndex == index)
                            return new ResolvedExportObject(exportIndex, pkg);
                }
            }

            return null;
        }

        private class ResolvedExportObject : ResolvedObject
        {
            public FExportMapEntry ExportMapEntry;
            public Lazy<UObject> ExportObject;

            public ResolvedExportObject(int exportIndex, IoPackage package) : base(package, exportIndex)
            {
                if (exportIndex >= package.ExportMap.Length) return;
                ExportMapEntry = package.ExportMap[exportIndex];
                ExportObject = package.ExportsLazy[exportIndex];
            }

            public override FName Name => ((IoPackage) Package).CreateFNameFromMappedName(ExportMapEntry.ObjectName);
            public override ResolvedObject Outer => ((IoPackage) Package).ResolveObjectIndex(ExportMapEntry.OuterIndex) ?? new ResolvedLoadedObject(Index, (UObject) Package);
            public override ResolvedObject? Class => ((IoPackage) Package).ResolveObjectIndex(ExportMapEntry.ClassIndex);
            public override ResolvedObject? Super => ((IoPackage) Package).ResolveObjectIndex(ExportMapEntry.SuperIndex);
            public override Lazy<UObject> Object => ExportObject;
        }

        private class ResolvedScriptObject : ResolvedObject
        {
            // public FScriptObjectEntry ScriptImport;
            public string ScriptImportName;

            public ResolvedScriptObject(string scriptImportName, IoPackage package) : base(package, 0)
            {
                ScriptImportName = scriptImportName;
            }

            public override FName Name => new(ScriptImportName);
            public override ResolvedObject? Outer => null; //((IoPackage) Package).ResolveObjectIndex(ScriptImport.OuterIndex);
            public override ResolvedObject Class => new ResolvedLoadedObject(Index, new UScriptClass("Class"));
            public override Lazy<UObject> Object => new(() => new UScriptClass(Name.Text));
        }
    }
}