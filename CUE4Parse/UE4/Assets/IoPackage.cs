using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Assets;

/// <summary>
/// The optional segment of an IoStore package
/// </summary>
public sealed record IoOptionalSegment(FArchive Archive, Func<FByteBulkDataHeader?, FArchive?>? Ubulk = null, Func<FByteBulkDataHeader?, FArchive?>? Uptnl = null, FIoContainerHeader? ContainerHeader = null);

[SkipObjectRegistration]
public sealed class IoPackage : AbstractUePackage
{
    private readonly IoGlobalData _globalData;
    private readonly EGame _game;

    public override FPackageFileSummary Summary { get; }
    public override FNameEntrySerialized[] NameMap { get; }
    public override int ImportMapLength => ImportMap.Length;

    /// <summary>
    /// Number of exports of this package. When an optional segment is merged in, its exports
    /// directly follow the ones of the base realm.
    /// </summary>
    public override int ExportMapLength { get; }

    public readonly ulong[]? ImportedPublicExportHashes;
    public readonly FPackageObjectIndex[] ImportMap;
    public readonly FExportMapEntry[] ExportMap;
    public readonly FBulkDataMapEntry[] BulkDataMap;
    public readonly Lazy<IoPackage?[]> ImportedPackages;
    public readonly Lazy<IPackage?[][]> ImportedPackagesAllVersions;

    /// <summary>
    /// The save realms of this package, base realm first. Unreal Engine cooks an IoStore package as
    /// one or more independent zen headers ("realms") and merges them into a single package when it
    /// loads them, so we keep one <see cref="RealmData"/> per header and expose the base realm's data
    /// through the public fields for compatibility.
    /// </summary>
    private readonly RealmData[] _realms;

    private RealmData BaseRealm
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _realms[0];
    }

    private RealmData? OptionalRealm
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _realms.Length > 1 ? _realms[1] : null;
    }

    /// <summary>Whether an optional segment has been merged into this package.</summary>
    public bool HasOptionalSegment => OptionalRealm is not null;

    /// <summary>Number of exports contributed by the merged optional segment.</summary>
    public int OptionalSegmentExportCount => OptionalRealm?.ExportMap.Length ?? 0;

    public IoPackage(FArchive uasset, FIoContainerHeader? containerHeader = null, FArchive? ubulk = null, FArchive? uptnl = null, IVfsFileProvider? provider = null)
        : this(uasset, containerHeader, ubulk, uptnl, provider, null)
    {
    }

    public IoPackage(FArchive uasset, FIoContainerHeader? containerHeader, FArchive? ubulk, FArchive? uptnl, IVfsFileProvider? provider, IoOptionalSegment? optionalSegment)
        : this(
            uasset,
            containerHeader,
            ubulk != null ? _ => ubulk : null,
            uptnl != null ? _ => uptnl : null,
            provider,
            optionalSegment)
    {
    }

    public IoPackage(
        FArchive uasset,
        FIoContainerHeader? containerHeader = null,
        Func<FByteBulkDataHeader?, FArchive?>? ubulk = null,
        Func<FByteBulkDataHeader?, FArchive?>? uptnl = null,
        IVfsFileProvider? provider = null,
        IoOptionalSegment? optionalSegment = null)
        : base(uasset.Name.SubstringBeforeLast('.'), provider)
    {
        _globalData = provider?.GlobalData ?? throw new ParserException("Found IoStore Package but global data is missing, can't serialize");

        var baseRealm = ReadRealm(uasset, containerHeader, provider, false, 0);
        _game = baseRealm.Archive.Game;
        var optionalRealm = optionalSegment is null
            ? null
            : ReadRealm(optionalSegment.Archive, optionalSegment.ContainerHeader ?? containerHeader, provider, true, 1);

        _realms = optionalRealm is null ? [baseRealm] : [baseRealm, optionalRealm];

        var exportBaseIndex = 0;
        foreach (var realm in _realms)
        {
            realm.ExportBaseIndex = exportBaseIndex;
            exportBaseIndex += realm.ExportMap.Length;
        }
        ExportMapLength = exportBaseIndex;

        Name = baseRealm.Name;
        Summary = baseRealm.Summary;
        Summary.ExportCount = ExportMapLength;
        NameMap = baseRealm.NameMap;
        ImportMap = baseRealm.ImportMap;
        ExportMap = baseRealm.ExportMap;
        BulkDataMap = baseRealm.BulkDataMap;
        ImportedPublicExportHashes = baseRealm.ImportedPublicExportHashes;
        ImportedPackages = baseRealm.ImportedPackages;
        ImportedPackagesAllVersions = baseRealm.ImportedPackagesAllVersions;

        if (!CanDeserialize) return;

        // Attach ubulk and uptnl
        if (ubulk != null) baseRealm.Archive.AddPayload(PayloadType.UBULK, Summary.BulkDataStartOffset, ubulk);
        if (uptnl != null) baseRealm.Archive.AddPayload(PayloadType.UPTNL, Summary.BulkDataStartOffset, uptnl);
        if (optionalRealm is not null && optionalSegment is not null)
        {
            if (optionalSegment.Ubulk != null) optionalRealm.Archive.AddPayload(PayloadType.UBULK, Summary.BulkDataStartOffset, optionalSegment.Ubulk);
            if (optionalSegment.Uptnl != null) optionalRealm.Archive.AddPayload(PayloadType.UPTNL, Summary.BulkDataStartOffset, optionalSegment.Uptnl);
        }

        // Populate lazy exports, the optional segment directly follows the base realm exports
        ExportsLazy = new Lazy<UObject>[ExportMapLength];
        foreach (var realm in _realms) PopulateExports(realm);

        IsFullyLoaded = true;
    }

    private RealmData ReadRealm(FArchive uasset, FIoContainerHeader? containerHeader, IVfsFileProvider? provider, bool isOptionalSegment, int realmIndex)
    {
        var realm = new RealmData();
        var uassetAr = realm.Archive = new FAssetArchive(uasset, this);
        uassetAr.RealmIndex = realmIndex;

        FExportBundleHeader[]? exportBundleHeaders;
        FExportBundleEntry[] exportBundleEntries;
        int cookedHeaderSize;
        int allExportDataOffset;

        if (uassetAr.Game >= GAME_UE5_0)
        {
            // Summary
            var summary = new FZenPackageSummary(uassetAr);
            realm.Summary = new FPackageFileSummary
            {
                PackageFlags = summary.PackageFlags,
                TotalHeaderSize = summary.GraphDataOffset + (int) summary.HeaderSize,
                NameOffset = (int) uassetAr.Position,
                ExportCount = (summary.ExportBundleEntriesOffset - summary.ExportMapOffset) / FExportMapEntry.Size,
                ExportOffset = summary.ExportMapOffset,
                ImportCount = (summary.ExportMapOffset - summary.ImportMapOffset) / FPackageObjectIndex.Size,
                ImportOffset = summary.ImportMapOffset,
            };

            // Versioning info
            if (summary.bHasVersioningInfo != 0)
            {
                var versioningInfo = new FZenPackageVersioningInfo(uassetAr);
                realm.Summary.FileVersionUE = versioningInfo.PackageVersion;
                realm.Summary.FileVersionLicenseeUE = (EUnrealEngineObjectLicenseeUEVersion) versioningInfo.LicenseeVersion;
                realm.Summary.CustomVersionContainer = versioningInfo.CustomVersions;
                if (!uassetAr.Versions.bExplicitVer)
                {
                    uassetAr.Versions.Ver = versioningInfo.PackageVersion;
                    uassetAr.Versions.CustomVersions = versioningInfo.CustomVersions;
                }
            }
            else
            {
                realm.Summary.bUnversioned = true;
            }

            FZenPackageCellOffsets cellOffsets;
            if (uassetAr.Ver >= EUnrealEngineObjectUE5Version.VERSE_CELLS)
            {
                cellOffsets = uassetAr.Read<FZenPackageCellOffsets>();
            }
            else
            {
                cellOffsets.CellImportMapOffset = summary.ExportBundleEntriesOffset;
                cellOffsets.CellExportMapOffset = summary.ExportBundleEntriesOffset;
            }

            // Name map
            realm.NameMap = FNameEntrySerialized.LoadNameBatch(uassetAr);
            realm.Summary.NameCount = realm.NameMap.Length;
            realm.Name = CreateFNameFromMappedName(summary.Name, realm).Text;
            realm.PackageId = FPackageId.FromName(realm.Name);

            realm.BulkDataMap = [];
            if (uassetAr.Ver >= EUnrealEngineObjectUE5Version.DATA_RESOURCES || uassetAr.Game == GAME_TheFirstDescendant)
            {
                if (uassetAr.Game >= GAME_UE5_4)
                {
                    var pad = uassetAr.Read<ulong>(); // pad
                    _ = uassetAr.ReadArray<byte>((int) pad);
                }

                var bulkDataMapSize = uassetAr.Read<long>();
                realm.BulkDataMap = uassetAr.ReadArray<FBulkDataMapEntry>((int) (bulkDataMapSize / FBulkDataMapEntry.Size));
            }

            // Imported public export hashes
            if (uassetAr.Game is not (GAME_UE5_EA_Legacy or GAME_TheMatrixAwakens))
            {
                uassetAr.Position = summary.ImportedPublicExportHashesOffset;
                realm.ImportedPublicExportHashes = uassetAr.ReadArray<ulong>((summary.ImportMapOffset - summary.ImportedPublicExportHashesOffset) / sizeof(ulong));
            }
            else
            {
                realm.ImportedPublicExportHashes = null;
            }

            // Import map
            uassetAr.Position = summary.ImportMapOffset;
            realm.ImportMap = uasset.ReadArray<FPackageObjectIndex>(realm.Summary.ImportCount);

            // Export map
            uassetAr.Position = summary.ExportMapOffset;
            realm.ExportMap = uasset.ReadArray(realm.Summary.ExportCount, () => new FExportMapEntry(uassetAr));

            // Export bundle entries
            uassetAr.Position = cellOffsets.CellImportMapOffset;
            exportBundleEntries = uassetAr.ReadArray<FExportBundleEntry>(realm.Summary.ExportCount * 2);

            (var storeEntry, realm.ImportedPackageIds) = GetStoreEntryAndImportedPackageIds(realm, containerHeader, provider, isOptionalSegment);
            if (uassetAr.Game < GAME_UE5_3)
            {
                // Export bundle headers
                uassetAr.Position = summary.GraphDataOffset;
                var exportBundleHeadersCount = storeEntry?.ExportBundleCount ?? 1;
                exportBundleHeaders = uassetAr.ReadArray<FExportBundleHeader>(exportBundleHeadersCount);
                // We don't read the graph data
            }
            else exportBundleHeaders = null;

            cookedHeaderSize = (int) summary.CookedHeaderSize;
            allExportDataOffset = (int) summary.HeaderSize;
        }
        else
        {
            // Summary
            var summary = uassetAr.Read<FPackageSummary>();
            realm.Summary = new FPackageFileSummary
            {
                PackageFlags = summary.PackageFlags,
                TotalHeaderSize = summary.GraphDataOffset + summary.GraphDataSize,
                NameCount = summary.NameMapHashesSize / sizeof(ulong) - 1,
                NameOffset = summary.NameMapNamesOffset,
                ExportCount = (summary.ExportBundlesOffset - summary.ExportMapOffset) / FExportMapEntry.Size,
                ExportOffset = summary.ExportMapOffset,
                ImportCount = (summary.ExportMapOffset - summary.ImportMapOffset) / FPackageObjectIndex.Size,
                ImportOffset = summary.ImportMapOffset,
                bUnversioned = true
            };

            // Name map
            uassetAr.Position = summary.NameMapNamesOffset;
            realm.NameMap = FNameEntrySerialized.LoadNameBatch(uassetAr, realm.Summary.NameCount);
            realm.Name = CreateFNameFromMappedName(summary.Name, realm).Text;
            realm.PackageId = FPackageId.FromName(realm.Name);

            // Import map
            uassetAr.Position = summary.ImportMapOffset;
            realm.ImportMap = uasset.ReadArray<FPackageObjectIndex>(realm.Summary.ImportCount);

            // Export map
            uassetAr.Position = summary.ExportMapOffset;
            realm.ExportMap = uasset.ReadArray(realm.Summary.ExportCount, () => new FExportMapEntry(uassetAr));

            // Export bundles
            uassetAr.Position = summary.ExportBundlesOffset;
            LoadExportBundles(uassetAr, summary.GraphDataOffset - summary.ExportBundlesOffset, out exportBundleHeaders, out exportBundleEntries);

            // Graph data
            uassetAr.Position = summary.GraphDataOffset;
            realm.ImportedPackageIds = LoadGraphData(uassetAr);

            cookedHeaderSize = (int) summary.CookedHeaderSize;
            allExportDataOffset = summary.GraphDataOffset + summary.GraphDataSize;
        }

        realm.ExportBundleHeaders = exportBundleHeaders;
        realm.ExportBundleEntries = exportBundleEntries;
        realm.CookedHeaderSize = cookedHeaderSize;
        realm.AllExportDataOffset = allExportDataOffset;

        // Only set if optional segment to allow owner namemap by default
        if (isOptionalSegment)
        {
            realm.Archive.RealmNameMap = realm.NameMap;
            realm.Archive.BulkDataMap = realm.BulkDataMap;
        }

        // Preload dependencies
        realm.ImportedPackages = new Lazy<IoPackage?[]>(() =>
        {
            var packages = new IoPackage?[realm.ImportedPackageIds.Length];
            for (var i = 0; i < realm.ImportedPackageIds.Length; i++)
            {
                // An optional segment references exports of the package it belongs to, which is this one.
                // Resolving it through the file provider would build a second, unmerged instance of it.
                if (IsSelfReference(realm.ImportedPackageIds[i]))
                {
                    packages[i] = this;
                    continue;
                }

                provider?.TryLoadPackage(realm.ImportedPackageIds[i], out packages[i]);
            }
            return packages;
        });

        realm.ImportedPackagesAllVersions = new Lazy<IPackage?[][]>(() =>
        {
            var packages = new IPackage?[realm.ImportedPackageIds.Length][];
            for (var i = 0; i < realm.ImportedPackageIds.Length; i++)
            {
                if (IsSelfReference(realm.ImportedPackageIds[i]))
                {
                    packages[i] = [this];
                    continue;
                }

                var package = realm.ImportedPackages.Value[i];
                if (package == null)
                {
                    packages[i] = [];
                    continue;
                }

                packages[i] = provider?.TryLoadPackages(package.Name, out var packagesList) == true ? [.. packagesList] : [];
            }
            return packages;
        });

        return realm;

        bool IsSelfReference(FPackageId packageId) => packageId.Equals(realm.PackageId);
    }

    private (FFilePackageStoreEntry? storeEntry, FPackageId[] importedPackageIds) GetStoreEntryAndImportedPackageIds(
        RealmData realm, FIoContainerHeader? containerHeader, IVfsFileProvider? provider, bool isOptionalSegment)
    {
        // Find store entry by package name
        FFilePackageStoreEntry? storeEntry = null;
        var importedPackageIds = Array.Empty<FPackageId>();
        var packageId = realm.PackageId;

        if (containerHeader != null)
        {
            var storeEntryIdx = Array.IndexOf(containerHeader.PackageIds, packageId);
            var optionalSegmentStoreEntryIdx = containerHeader.OptionalSegmentPackageIds is { Length: > 0 }
                ? Array.IndexOf(containerHeader.OptionalSegmentPackageIds, packageId)
                : -1;

            // Both realms share the package id, their store entries live in separate arrays:
            // the optional segment one only exists in the container holding the optional segment.
            if (isOptionalSegment && optionalSegmentStoreEntryIdx != -1)
            {
                storeEntry = containerHeader.OptionalSegmentStoreEntries[optionalSegmentStoreEntryIdx];
            }
            else if (storeEntryIdx != -1)
            {
                storeEntry = containerHeader.StoreEntries[storeEntryIdx];
            }
            else if (optionalSegmentStoreEntryIdx != -1)
            {
                storeEntry = containerHeader.OptionalSegmentStoreEntries[optionalSegmentStoreEntryIdx];
            }
        }

        if (storeEntry is null)
        {
            // The store entry can live in another container than the one this chunk was read from.
            // The optional segment has its own entry holding its imported packages, so it must not
            // fall back to the entry of the package itself.
            var vfsProvider = provider as AbstractVfsFileProvider;
            storeEntry = isOptionalSegment
                ? vfsProvider?.TryFindOptionalSegmentStoreEntry(packageId)
                : vfsProvider?.TryFindStoreEntry(packageId);
        }

        if (storeEntry is null)
        {
            Log.Warning("Couldn't find store entry for package {RealmName}, its data will not be fully read", realm.Name);
            return (null, importedPackageIds);
        }

        importedPackageIds = storeEntry.ImportedPackages ?? [];
        return (storeEntry, importedPackageIds);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FName CreateFNameFromMappedName(FMappedName mappedName) => CreateFNameFromMappedName(mappedName, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FName CreateFNameFromMappedName(FMappedName mappedName, RealmData? realm) =>
        new(mappedName, mappedName.IsGlobal ? _globalData.GlobalNameMap : realm?.NameMap ?? NameMap);

    private void LoadExportBundles(FArchive Ar, int graphDataSize, out FExportBundleHeader[] bundleHeadersArray, out FExportBundleEntry[] bundleEntriesArray)
    {
        var remainingBundleEntryCount = graphDataSize / (4 + 4);
        var foundBundlesCount = 0;
        var foundBundleHeaders = new List<FExportBundleHeader>();
        while (foundBundlesCount < remainingBundleEntryCount)
        {
            // This location is occupied by header, so it is not a bundle entry
            remainingBundleEntryCount--;
            var bundleHeader = new FExportBundleHeader(Ar);
            foundBundlesCount += (int) bundleHeader.EntryCount;
            foundBundleHeaders.Add(bundleHeader);
        }

        if (foundBundlesCount != remainingBundleEntryCount)
            throw new ParserException(Ar, $"FoundBundlesCount {foundBundlesCount} != RemainingBundleEntryCount {remainingBundleEntryCount}");

        // Load export bundles into arrays
        bundleHeadersArray = [.. foundBundleHeaders];
        bundleEntriesArray = Ar.ReadArray<FExportBundleEntry>(foundBundlesCount);
    }

    private FPackageId[] LoadGraphData(FArchive Ar)
    {
        if (Ar.Game is GAME_NeedForSpeedMobile && Ar.ReadBoolean()) Ar.Position += 8;
        var packageCount = Ar.Read<int>();
        if (packageCount == 0) return [];

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

    /// <summary>
    /// Sets up the lazy exports of one realm. The exports of the base realm are placed first, the
    /// ones of the optional segment directly after them, matching UE's concatenated export table.
    /// </summary>
    private void PopulateExports(RealmData realm)
    {
        var uassetAr = realm.Archive;

        if (realm.ExportBundleHeaders != null) // 4.26 - 5.2
        {
            var currentExportDataOffset = realm.AllExportDataOffset;
            foreach (var exportBundle in realm.ExportBundleHeaders)
            {
                for (var i = 0u; i < exportBundle.EntryCount; i++)
                {
                    currentExportDataOffset += ProcessEntry(realm.ExportBundleEntries[exportBundle.FirstEntryIndex + i], currentExportDataOffset, false);
                }
                realm.Summary.BulkDataStartOffset = currentExportDataOffset;
            }
        }
        else foreach (var entry in realm.ExportBundleEntries)
        {
            ProcessEntry(entry, realm.AllExportDataOffset + (int) realm.ExportMap[entry.LocalExportIndex].CookedSerialOffset, true);
        }

        return;

        int ProcessEntry(FExportBundleEntry entry, int pos, bool newPos)
        {
            if (entry.CommandType != EExportCommandType.ExportCommandType_Serialize)
                return 0; // Skip ExportCommandType_Create

            var export = realm.ExportMap[entry.LocalExportIndex];
            ExportsLazy[realm.ExportBaseIndex + (int) entry.LocalExportIndex] = new Lazy<UObject>(() =>
            {
                // Create
                var obj = ConstructObject(ResolveObjectIndex(realm, export.ClassIndex), this, export.ObjectFlags);
                obj.Name = CreateFNameFromMappedName(export.ObjectName, realm).Text;
                obj.Outer = ResolveObjectIndex(realm, export.OuterIndex) as ResolvedExportObject;
                obj.Outer ??= new ResolvedPackageObject(this);
                obj.Super = ResolveObjectIndex(realm, export.SuperIndex) as ResolvedExportObject;
                obj.Template = ResolveObjectIndex(realm, export.TemplateIndex) as ResolvedExportObject;
                obj.Flags |= export.ObjectFlags; // We give loaded objects the RF_WasLoaded flag in ConstructObject, so don't remove it again in here

                // Serialize
                var Ar = (FAssetArchive) uassetAr.Clone();
                Ar.AbsoluteOffset = newPos ? realm.CookedHeaderSize - realm.AllExportDataOffset : (int) export.CookedSerialOffset - pos;
                Ar.Position = pos;
                DeserializeObject(obj, Ar, (long) export.CookedSerialSize);
                obj.Flags |= EObjectFlags.RF_LoadCompleted;
                obj.PostLoad();
                return obj;
            });
            return (int) export.CookedSerialSize;
        }
    }

    public override int GetExportIndex(string name, StringComparison comparisonType = StringComparison.Ordinal)
    {
        foreach (var realm in _realms)
        {
            for (var i = 0; i < realm.ExportMap.Length; i++)
            {
                if (CreateFNameFromMappedName(realm.ExportMap[i].ObjectName, realm).Text.Equals(name, comparisonType))
                {
                    return realm.ExportBaseIndex + i;
                }
            }
        }

        return -1;
    }

    public override ResolvedObject? ResolvePackageIndex(FPackageIndex? index)
    {
        if (index == null || index.IsNull)
            return null;
        var realm = RealmOf(index.RealmIndex);
        if (index.IsImport && -index.Index - 1 < realm.ImportMap.Length)
            return ResolveObjectIndex(realm, realm.ImportMap[-index.Index - 1]);
        if (index.IsExport && index.Index - 1 < realm.ExportMap.Length)
            return CreateResolvedExport(realm.ExportBaseIndex + index.Index - 1);
        return null;
    }

    /// <summary>
    /// Returns the realm a raw package index was read from, falling back to the base realm for archives
    /// that are not realm aware (anything that is not an export of this package's optional segment).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private RealmData RealmOf(int realmIndex)
        => realmIndex >= 0 && realmIndex < _realms.Length ? _realms[realmIndex] : BaseRealm;

    public ResolvedObject? ResolveObjectIndex(FPackageObjectIndex index) => ResolveObjectIndex(BaseRealm, index);

    /// <param name="realm">
    /// The save realm the index was read from. Export indices of an optional segment are local to
    /// that realm while imports of both realms resolve through their respective imported packages.
    /// </param>
    private ResolvedObject? ResolveObjectIndex(RealmData realm, FPackageObjectIndex index)
    {
        if (index.IsNull)
        {
            return null;
        }

        if (index.IsExport)
        {
            return CreateResolvedExport(realm.ExportBaseIndex + (int) index.AsExport);
        }

        if (index.IsScriptImport)
        {
            if (_globalData.ScriptObjectEntriesMap.TryGetValue(index, out var scriptObjectEntry))
            {
                return new ResolvedScriptObject(scriptObjectEntry, this);
            }
        }

        if (index.IsPackageImport)
        {
            var importedPublicExportHashes = realm.ImportedPublicExportHashes;
            var importedPackages = realm.ImportedPackages.Value;
            if (importedPublicExportHashes != null)
            {
                var packageImportRef = index.AsPackageImportRef;
                if (packageImportRef.ImportedPackageIndex < importedPackages.Length)
                {
                    var exportHash = importedPublicExportHashes[packageImportRef.ImportedPublicExportHashIndex];
                    var pkg = importedPackages[packageImportRef.ImportedPackageIndex];
                    if (pkg?.FindExportByPublicExportHash(exportHash) is { } export)
                    {
                        return export;
                    }

                    // search all previous versions
                    var importedPackagesAllVersions = realm.ImportedPackagesAllVersions.Value;
                    var packages = importedPackagesAllVersions[packageImportRef.ImportedPackageIndex];
                    foreach (var asset in packages)
                    {
                        switch (asset)
                        {
                            case IoPackage ioPackage when ioPackage.FindExportByPublicExportHash(exportHash) is { } ioExport:
                                return ioExport;
                            case Package package:
                            {
                                for (var exportIndex = 0; exportIndex < package.ExportMap.Length; ++exportIndex)
                                {
                                    if (package.ExportMap[exportIndex].GetPublicExportHash() == exportHash)
                                    {
                                        return new ResolvedPakExportObject(exportIndex, package);
                                    }
                                }

                                break;
                            }
                        }
                    }
                }
            }
            else if (_game is GAME_UE5_EA_Legacy or GAME_TheMatrixAwakens)
            {
                // Pre-finalization Zen: the low 32 bits of a package import reference are the target export's 32-bit export hash.
                var packageImportRef = index.AsPackageImportRef;
                if (packageImportRef.ImportedPackageIndex < importedPackages.Length)
                {
                    var pkg = importedPackages[packageImportRef.ImportedPackageIndex];
                    if (pkg != null)
                    {
                        for (var exportIndex = 0; exportIndex < pkg.ExportMap.Length; ++exportIndex)
                        {
                            if (pkg.ExportMap[exportIndex].PublicExportHash == packageImportRef.ImportedPublicExportHashIndex) // ExportHash
                            {
                                return pkg.CreateResolvedExport(exportIndex);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var pkg in importedPackages)
                {
                    if (pkg?.FindExportByGlobalImportIndex(index) is { } export)
                    {
                        return export;
                    }
                }

                // search all previous versions
                foreach (var packages in realm.ImportedPackagesAllVersions.Value)
                {
                    foreach (var asset in packages)
                    {
                        switch (asset)
                        {
                            case IoPackage ioPackage when ioPackage.FindExportByGlobalImportIndex(index) is { } ioExport:
                                return ioExport;
                            case Package package:
                            {
                                for (var exportIndex = 0; exportIndex < package.ExportMap.Length; ++exportIndex)
                                {
                                    if (package.ExportMap[exportIndex].GetGlobalImportIndex() == index)
                                    {
                                        return new ResolvedPakExportObject(exportIndex, package);
                                    }
                                }

                                break;
                            }
                        }
                    }
                }
            }
        }

        if (Globals.WarnMissingImportPackage)
        {
            Log.Warning("Missing {0} import 0x{1:X} for package {2}", index.IsScriptImport ? "script" : "package", index.Value, Name);
        }

        return null;
    }

    /// <summary>
    /// Resolves an export index of the unified export table (base realm first, then optional segment).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ResolvedObject CreateResolvedExport(int exportIndex)
    {
        foreach (var realm in _realms)
        {
            if (exportIndex < realm.ExportBaseIndex + realm.ExportMap.Length)
                return new ResolvedExportObject(exportIndex, this, realm);
        }

        // Index out of range of every realm, resolve against the base realm so the caller gets a
        // meaningful (empty) export instead of a wrong entry.
        return new ResolvedExportObject(exportIndex, this, BaseRealm);
    }

    private ResolvedObject? FindExportByPublicExportHash(ulong exportHash)
    {
        foreach (var realm in _realms)
        {
            for (var exportIndex = 0; exportIndex < realm.ExportMap.Length; exportIndex++)
            {
                if (realm.ExportMap[exportIndex].PublicExportHash == exportHash)
                    return CreateResolvedExport(realm.ExportBaseIndex + exportIndex);
            }
        }

        return null;
    }

    private ResolvedObject? FindExportByGlobalImportIndex(FPackageObjectIndex index)
    {
        foreach (var realm in _realms)
        {
            for (var exportIndex = 0; exportIndex < realm.ExportMap.Length; exportIndex++)
            {
                if (realm.ExportMap[exportIndex].GlobalImportIndex == index)
                    return CreateResolvedExport(realm.ExportBaseIndex + exportIndex);
            }
        }

        return null;
    }

    /// <summary>Cooked data of one save realm (base package or optional segment) of this package.</summary>
    private sealed class RealmData
    {
        public FAssetArchive Archive = null!;
        public FPackageFileSummary Summary = null!;
        public string Name = string.Empty;
        public FPackageId PackageId;
        public FNameEntrySerialized[] NameMap = null!;
        public FPackageObjectIndex[] ImportMap = null!;
        public FExportMapEntry[] ExportMap = null!;
        public FBulkDataMapEntry[] BulkDataMap = null!;
        public ulong[]? ImportedPublicExportHashes;
        public FPackageId[] ImportedPackageIds = [];
        public FExportBundleHeader[]? ExportBundleHeaders;
        public FExportBundleEntry[] ExportBundleEntries = null!;
        public int CookedHeaderSize;
        public int AllExportDataOffset;
        public int ExportBaseIndex;
        public Lazy<IoPackage?[]> ImportedPackages = null!;
        public Lazy<IPackage?[][]> ImportedPackagesAllVersions = null!;
    }

    private class ResolvedPakExportObject(int exportIndex, Package package) : ResolvedObject(package, exportIndex)
    {
        private readonly FObjectExport _export = package.ExportMap[exportIndex];

        public override FName Name => _export.ObjectName;
        public override ResolvedObject Outer => Package.ResolvePackageIndex(_export.OuterIndex) ?? new ResolvedPackageObject(Package);
        public override ResolvedObject? Class => Package.ResolvePackageIndex(_export.ClassIndex);
        public override ResolvedObject? Super => Package.ResolvePackageIndex(_export.SuperIndex);
    }

    private class ResolvedExportObject : ResolvedObject
    {
        private readonly IoPackage _package;
        private readonly RealmData _realm;
        private readonly FExportMapEntry? _exportMapEntry;

        public ResolvedExportObject(int exportIndex, IoPackage package, RealmData realm) : base(package, exportIndex)
        {
            _package = package;
            _realm = realm;

            var localExportIndex = exportIndex - realm.ExportBaseIndex;
            if (localExportIndex >= 0 && localExportIndex < realm.ExportMap.Length)
                _exportMapEntry = realm.ExportMap[localExportIndex];
        }

        public override FName Name => _exportMapEntry is { } entry ? _package.CreateFNameFromMappedName(entry.ObjectName, _realm) : "None";
        public override ResolvedObject Outer => _exportMapEntry is { } entry ? _package.ResolveObjectIndex(_realm, entry.OuterIndex) ?? new ResolvedPackageObject(_package) : new ResolvedPackageObject(_package);
        public override ResolvedObject? Class => _exportMapEntry is { } entry ? _package.ResolveObjectIndex(_realm, entry.ClassIndex) : null;
        public override ResolvedObject? Super => _exportMapEntry is { } entry ? _package.ResolveObjectIndex(_realm, entry.SuperIndex) : null;
    }

    private class ResolvedScriptObject(FScriptObjectEntry scriptImport, IoPackage package) : ResolvedObject(package)
    {
        public readonly FScriptObjectEntry ScriptImport = scriptImport;

        public override FName Name => ((IoPackage) Package).CreateFNameFromMappedName(ScriptImport.ObjectName);
        public override ResolvedObject? Outer => ((IoPackage) Package).ResolveObjectIndex(ScriptImport.OuterIndex);
        // This means we'll have UScriptStruct's shown as UClass which is wrong.
        // Unfortunately because the mappings format does not distinguish between classes and structs, there's no other way around :(
        public override ResolvedObject Class => new ResolvedLoadedObject(new UScriptClass("Class"));
        public override Lazy<UObject> Object => new(() => new UScriptClass(Name.Text));
    }

    public static string GetIoPackageName(FArchive uasset)
    {
        var uassetAr = new FAssetArchive(uasset, null);
        var summary = new FZenPackageSummary(uassetAr);
        var nameMap = FNameEntrySerialized.LoadNameBatch(uassetAr);
        return new FName(summary.Name, nameMap).Text[1..];
    }
}
