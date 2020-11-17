using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Assets
{
    public sealed class IoPackage : AbstractUePackage
    {
        public readonly FPackageSummary IoSummary;
        public readonly IoGlobalData GlobalData;
        public override FPackageFileSummary Summary { get; }
        public override FNameEntrySerialized[] NameMap { get; }
        public override FObjectImport[] ImportMap { get; }
        public override FObjectExport[] ExportMap { get; }

        public IoPackage(FArchive uasset, IoGlobalData globalData, FArchive? ubulk = null, FArchive? uptnl = null, IFileProvider? provider = null, TypeMappings? mappings = null)
            : base(uasset.Name.SubstringBeforeLast(".uasset"), provider, mappings)
        {
            GlobalData = globalData;
            var uassetAr = new FAssetArchive(uasset, this);
            IoSummary = uassetAr.Read<FPackageSummary>();
            Summary = new FPackageFileSummary
            {
                PackageFlags = (PackageFlags) IoSummary.PackageFlags
            };

            var headerSize = IoSummary.GraphDataOffset + IoSummary.GraphDataSize;
            Summary.TotalHeaderSize = headerSize;

            uassetAr.Position = IoSummary.NameMapNamesOffset;
            var nameCount = IoSummary.NameMapHashesSize / sizeof(ulong) - 1;
            Summary.NameCount = nameCount;
            NameMap = FNameEntrySerialized.LoadNameBatch(uassetAr, nameCount);

            LoadExportBundles(uassetAr, out var bundleHeadersArray, out var bundleEntriesArray);

            var exportTableSize = IoSummary.ExportBundlesOffset - IoSummary.ExportMapOffset;
            var exportCount = exportTableSize / Unsafe.SizeOf<FExportMapEntry>();
            Summary.ExportCount = exportCount;
            ExportMap = LoadExportTable(uassetAr, exportCount, exportTableSize, headerSize, bundleHeadersArray,
                bundleEntriesArray);

            if (ubulk != null)
            {
                var offset = Summary.BulkDataStartOffset;
                var ubulkAr = new FAssetArchive(ubulk, this, offset);
                uassetAr.AddPayload(PayloadType.UBULK, ubulkAr);
            }
            if (uptnl != null)
            {
                var offset = Summary.BulkDataStartOffset;
                var uptnlAr = new FAssetArchive(uptnl, this, offset);
                uassetAr.AddPayload(PayloadType.UPTNL, uptnlAr);
            }
            
            ProcessExportMap(uassetAr);
        }

        private FObjectExport[] LoadExportTable(FArchive reader, int exportCount, int exportTableSize,
            int packageHeaderSize, FExportBundleHeader[] bundleHeaders, FExportBundleEntry[] bundleEntries)
        {
            var exportMap = new FObjectExport[exportCount];
            for (var i = 0; i < exportMap.Length; i++)
                exportMap[i] = new FObjectExport();

            reader.Position = IoSummary.ExportMapOffset;
            var exportEntries = reader.ReadArray<FExportMapEntry>(exportCount);
            
            // Export data is ordered according to export bundles, so we should do the processing in bundle order
            var currentExportOffset = packageHeaderSize;
            foreach (var bundleHeader in bundleHeaders)
            {
                for (var entryIndex = 0; entryIndex < bundleHeader.EntryCount; entryIndex++)
                {
                    var entry = bundleEntries[bundleHeader.FirstEntryIndex + entryIndex];
                    if (entry.CommandType == EExportCommandType.ExportCommandType_Serialize)
                    {
                        var objectIndex = entry.LocalExportIndex;

                        ref var e = ref exportEntries[objectIndex];
                        ref var exp = ref exportMap[objectIndex];
                        
                        // TODO: FExportMapEntry has FilterFlags which could affect inclusion of exports
                        if (e.CookedSerialOffset >= 0x7FFFFFFF || e.CookedSerialSize >= 0x7FFFFFFF)
                            throw new ParserException("TODO: FExportMapEntry has FilterFlags");

                        //This export offset is not the "real" offset
                        exp.SerialOffset = (long) e.CookedSerialOffset;
                        exp.RealSerialOffset = currentExportOffset;
                        exp.SerialSize = (long) e.CookedSerialSize;
                        exp.ObjectName = CreateFNameFromMappedName(e.ObjectName);
                        exp.ClassName = GlobalData.FindScriptEntryName(e.ClassIndex);
                        
                        currentExportOffset += (int) exp.SerialSize;
                    }
                }
            }

            Summary.BulkDataStartOffset = currentExportOffset;

            return exportMap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FName CreateFNameFromMappedName(FMappedName mappedName) =>
            new FName(mappedName, mappedName.IsGlobal ? GlobalData.GlobalNameMap : NameMap);

        private void LoadExportBundles(FArchive Ar, out FExportBundleHeader[] bundleHeadersArray,
            out FExportBundleEntry[] bundleEntriesArray)
        {
            Ar.Position = IoSummary.ExportBundlesOffset;
            var bundleHeadersBytes = Ar.ReadBytes(IoSummary.GraphDataOffset - IoSummary.ExportBundlesOffset);
            
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
                        throw new ParserException(Ar, $"FoundBundlesCount {foundBundlesCount} != RemainingBundleEntryCount {remainingBundleEntryCount}");
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
    }
}