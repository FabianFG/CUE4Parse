using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.Assets.Exports.Materials;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Textures;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Serilog;

namespace CUE4Parse.UE4.Assets
{
    public abstract class AbstractUePackage : IPackage
    {
        public string Name { get; }
        public IFileProvider? Provider { get; }
        public TypeMappings? Mappings { get; }
        public abstract FPackageFileSummary Summary { get; }
        public abstract FNameEntrySerialized[] NameMap { get; }
        public abstract FObjectImport[] ImportMap { get; }
        public abstract FObjectExport[] ExportMap { get; }

        public AbstractUePackage(string name, IFileProvider? provider, TypeMappings? mappings)
        {
            Name = name;
            Provider = provider;
            Mappings = mappings;
        }

        protected void ProcessExportMap(FAssetArchive exportAr)
        {
            foreach (var it in ExportMap)
            {
                var exportType = (it.ClassName == string.Empty || it.ClassName == "None") && !(this is IoPackage) ? exportAr.ReadFName().Text : it.ClassName;
                var export = ConstructExport(exportType, it);
                it.ExportType = export.GetType();
                it.ExportObject = new Lazy<UExport>(() =>
                {
                    exportAr.SeekAbsolute(it.RealSerialOffset, SeekOrigin.Begin);
                    var validPos = exportAr.Position + it.SerialSize;
                    try
                    {
                        export.Deserialize(exportAr, validPos);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, $"Could not read {exportType} correctly");
                    }
#if DEBUG
                    if (validPos != exportAr.Position)
                        Log.Warning($"Did not read {exportType} correctly, {validPos - exportAr.Position} bytes remaining");
                    else
                        Log.Debug($"Successfully read {exportType} at {it.RealSerialOffset} with size {it.SerialSize}");
#endif

                    return export;
                });
            }
        }
        
        private UExport ConstructExport(string exportType, FObjectExport export)
        {
            var result = exportType switch
            {
                "Texture2D" => new UTexture2D(export),
                "VirtualTexture2D" => new UTexture2D(export),
                "CurveTable" => new UCurveTable(export),
                "DataTable" => new UDataTable(export),
                "SoundWave" => new USoundWave(export),
                "StringTable" => new UStringTable(export),
                "Skeleton" => new USkeleton(export),
                "AkMediaAssetData" => new UAkMediaAssetData(export),
                "Material" => new UMaterial(export),
                "MaterialInstanceConstant" => new UMaterialInstanceConstant(export),
                _ => new UObject(export)
            };
            result.Owner = this;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasFlags(PackageFlags flags) => Summary.PackageFlags.HasFlag(flags);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetExportOfTypeOrNull<T>() where T : UExport
        {
            var export = ExportMap.FirstOrDefault(it => typeof(T).IsAssignableFrom(it.ExportType));
            try
            {
                return export?.ExportObject.Value as T;
            }
            catch(Exception e)
            {
                Log.Debug(e, "Failed to get export object");
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetExportOfType<T>() where T : UExport =>
            GetExportOfTypeOrNull<T>() ??
            throw new NullReferenceException($"Package '{Name}' does not have an export of type {typeof(T).Name}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UExport? GetExportOrNull(string name, StringComparison comparisonType = StringComparison.Ordinal)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetExportOrNull<T>(string name, StringComparison comparisonType = StringComparison.Ordinal)
            where T : UExport => GetExportOrNull(name, comparisonType) as T;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UExport GetExport(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            GetExportOrNull(name, comparisonType) ??
            throw new NullReferenceException(
                $"Package '{Name}' does not have an export with the name '{name}'");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<UExport> GetExports()
        {
            return ExportMap.Select(x => x.ExportObject.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetExport<T>(string name, StringComparison comparisonType = StringComparison.Ordinal)
            where T : UExport => GetExportOrNull<T>(name, comparisonType) ??
                                 throw new NullReferenceException(
                                     $"Package '{Name}' does not have an export with the name '{name} and type {typeof(T).Name}'");

        public override string ToString() => Name;
    }
}