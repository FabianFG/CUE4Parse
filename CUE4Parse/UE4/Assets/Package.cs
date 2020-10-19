using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Textures;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Serilog;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse.UE4.Assets
{
    public class Package
    {
        public static readonly uint PackageMagic = 0x9E2A83C1u;

        public readonly string Name;
        public readonly FPackageFileSummary Summary;
        public readonly FNameEntry[] NameMap;
        public readonly FObjectImport[] ImportMap;
        public readonly FObjectExport[] ExportMap;
        public readonly IFileProvider? Provider;

        public Package(FArchive uasset, FArchive uexp, FArchive? ubulk = null, FArchive? uptnl = null, IFileProvider? provider = null)
        {
            Name = uasset.Name.SubstringBeforeLast(".uasset");
            Provider = provider;
            var uassetAr = new FAssetArchive(uasset, this);
            Summary = new FPackageFileSummary(uassetAr);
            if (Summary.Tag != PackageMagic)
            {
                throw new ParserException(uassetAr, $"Invalid uasset magic: {Summary.Tag} != {PackageMagic}");
            }

            uassetAr.Seek(Summary.NameOffset, SeekOrigin.Begin);
            NameMap = uassetAr.ReadArray(Summary.NameCount, () => new FNameEntry(uassetAr));

            uassetAr.Seek(Summary.ImportOffset, SeekOrigin.Begin);
            ImportMap = uassetAr.ReadArray(Summary.ImportCount, () => new FObjectImport(uassetAr));

            uassetAr.Seek(Summary.ExportOffset, SeekOrigin.Begin);
            ExportMap = uassetAr.ReadArray(Summary.ExportCount, () => new FObjectExport(uassetAr));

            var uexpAr = new FAssetArchive(uexp, this, Summary.TotalHeaderSize);
            if (ubulk != null)
            {
                var offset = (int) (Summary.TotalHeaderSize + ExportMap.Sum(export => export.SerialSize));
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

            foreach (var it in ExportMap)
            {
                var exportType = 
                    it.ClassIndex.IsNull ? uexpAr.ReadFName().Text :
                    //it.ClassIndex.IsExport ? ExportMap[it.ClassIndex.Index - 1].SuperIndex.Name :
                    //it.ClassIndex.IsImport ? ImportMap[-it.ClassIndex.Index - 1].ObjectName.Text :
                    it.ClassIndex.Name;
                var export = ConstructExport(exportType, it);
                it.ExportType = export.GetType();
                it.ExportObject = new Lazy<UExport>(() =>
                {
                    uexpAr.SeekAbsolute(it.SerialOffset, SeekOrigin.Begin);
#if DEBUG
                    var validPos = uexpAr.Position + it.SerialSize;
#endif
                    export.Owner = this;
                    export.Deserialize(uexpAr);

#if DEBUG
                    if (validPos != uexpAr.Position)
                        Log.Warning($"Did not read {exportType} correctly, {validPos - uexpAr.Position} bytes remaining");
                    else
                        Log.Debug($"Successfully read {exportType} at {it.SerialOffset - Summary.TotalHeaderSize} with size {it.SerialSize}");
                        
#endif

                    return export;
                });
            }
        }

        public Package(string name, byte[] uasset, byte[] uexp, byte[]? ubulk = null, byte[]? uptnl = null, IFileProvider? provider = null)
            : this(new FByteArchive($"{name}.uasset", uasset), new FByteArchive($"{name}.uexp", uexp),
                ubulk != null ? new FByteArchive($"{name}.ubulk", ubulk) : null,
                uptnl != null ? new FByteArchive($"{name}.uptnl", uptnl) : null, provider)
        {
        }

        private UExport ConstructExport(string exportType, FObjectExport export)
        {
            return exportType switch
            {
                "Texture2D" => new UTexture2D(export),
                "VirtualTexture2D" => new UTexture2D(export),
                "CurveTable" => new UCurveTable(export),
                "DataTable" => new UDataTable(export),
                "SoundWave" => new USoundWave(export),
                "StringTable" => new UStringTable(export),
                "Skeleton" => new USkeleton(export),
                _ => new UObject(export, true)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetExportOfTypeOrNull<T>() where T : UExport
        {
            var export = ExportMap.FirstOrDefault(it => typeof(T).IsAssignableFrom(it.ExportType));
            return export?.ExportObject.Value as T;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetExportOfType<T>() where T : UExport =>
            GetExportOfTypeOrNull<T>() ??
            throw new NullReferenceException($"Package '{Name}' does not have an export of type {typeof(T).Name}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UExport? GetExportOrNull(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            ExportMap
                .FirstOrDefault(it => it.ObjectName.Text.Equals(name, comparisonType))?.ExportObject
                .Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetExportOrNull<T>(string name, StringComparison comparisonType = StringComparison.Ordinal)
            where T : UExport => GetExportOrNull(name, comparisonType) as T;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UExport GetExport(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            GetExportOrNull(name, comparisonType) ??
            throw new NullReferenceException(
                $"Package '{Name}' does not have an export with the name '{name}'");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetExport<T>(string name, StringComparison comparisonType = StringComparison.Ordinal)
            where T : UExport => GetExportOrNull<T>(name, comparisonType) ??
                                 throw new NullReferenceException(
                                     $"Package '{Name}' does not have an export with the name '{name} and type {typeof(T).Name}'");

        public override string ToString() => Name;
    }
}