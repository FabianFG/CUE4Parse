using System;
using System.IO;
using System.Linq;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets
{
    public class Package
    {
        public static readonly uint PackageMagic = 0x9E2A83C1u;

        public readonly FNameEntry[] NameMap;
        public readonly FObjectImport[] ImportMap;
        public readonly FObjectExport[] ExportMap;
        public Package(FArchive uasset, FArchive uexp, FArchive? ubulk)
        {
            var uassetAr = new FAssetArchive(uasset, this);
            var info = new FPackageFileSummary(uassetAr);
            if (info.Tag != PackageMagic)
            {
                throw new ParserException(uassetAr, $"Invalid uasset magic: {info.Tag} != {PackageMagic}");
            }

            uassetAr.Seek(info.NameOffset, SeekOrigin.Begin);
            NameMap = uassetAr.ReadArray(info.NameCount, () => new FNameEntry(uassetAr));

            uassetAr.Seek(info.ImportOffset, SeekOrigin.Begin);
            ImportMap = uassetAr.ReadArray(info.ImportCount, () => new FObjectImport(uassetAr));
            
            uassetAr.Seek(info.ExportOffset, SeekOrigin.Begin);
            ExportMap = uassetAr.ReadArray(info.ExportCount, () => new FObjectExport(uassetAr));
            
            var uexpAr = new FAssetArchive(uexp, this, info.TotalHeaderSize);
            if (ubulk != null)
            {
                var offset = (int) (info.TotalHeaderSize + ExportMap.Sum(export => export.SerialSize));
                var ubulkAr = new FAssetArchive(ubulk, this, offset);
                uexpAr.AddPayload(PayloadType.UBULK, ubulkAr);
            }
            
            foreach (var it in ExportMap)
            {
                uexpAr.SeekAbsolute(it.SerialOffset, SeekOrigin.Begin);
#if DEBUG
                var validPos = uexpAr.Position + it.SerialSize;
#endif
                var exportType = it.ClassIndex.IsNull ? uexpAr.ReadFName().Text : it.ClassIndex.Name;
                var export = ReadExport(exportType, it);
                export.Owner = this;
                export.Deserialize(uexpAr);
#if DEBUG
                Console.WriteLine(validPos != uexpAr.Position
                    ? $"Did not read {exportType} correctly, {validPos - uexpAr.Position} bytes remaining"
                    : $"Successfully read {exportType} at {it.SerialOffset - info.TotalHeaderSize} with size {it.SerialSize}");
#endif
            }
        }

        public Package(string name, byte[] uasset, byte[] uexp, byte[]? ubulk)
            : this(new FByteArchive($"{name}.uasset", uasset), new FByteArchive($"{name}.uexp", uexp),
                ubulk != null ? new FByteArchive($"{name}.ubulk", ubulk) : null)
        { }

        private UExport ReadExport(string exportType, FObjectExport export)
        {
            return exportType switch
            {
                _ => new UObject(export, false)
            };
        }
    }
}