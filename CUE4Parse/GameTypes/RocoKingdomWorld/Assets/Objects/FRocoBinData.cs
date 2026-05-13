using System;
using System.Collections.Generic;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using Serilog;

namespace CUE4Parse.GameTypes.RocoKingdomWorld.Assets.Objects;

public enum ERocoBinDataType : uint
{
    BinDataCompressed = 0,
    BinData = 1,
    BinLocalize = 2
}

public struct FRocoBinTable(FArchive Ar)
{
    public uint Index = Ar.Read<uint>();
    public int Length = Ar.Read<int>();
    public long Offset = Ar.Read<long>();
}

public struct FRocoBinFooter
{
    public long DataSectionOffset;
    public long DataSectionLength;

    public int EntriesCount;
    public long StructSize;

    public long DataTableOffset;
    public int DataTableEntriesCount;

    public long ConstantsTableOffset;
    public int ConstantsTableEntriesCount;
    public long ConstantsSectionOffset;
    public long ConstantsSectionLength;

    public FRocoBinFooter(FArchive Ar, ERocoBinDataType type)
    {
        switch (type)
        {
            case ERocoBinDataType.BinDataCompressed:
                DataSectionOffset = Ar.Read<long>();
                DataSectionLength = Ar.Read<long>();
                EntriesCount = Ar.Read<int>();
                StructSize = Ar.Read<long>();
                DataTableOffset = Ar.Read<long>();
                DataTableEntriesCount = Ar.Read<int>();
                ConstantsTableOffset = Ar.Read<long>();
                ConstantsTableEntriesCount = Ar.Read<int>();
                ConstantsSectionOffset = Ar.Read<long>();
                ConstantsSectionLength = Ar.Read<long>();
                break;
            case ERocoBinDataType.BinData:
                DataSectionOffset = Ar.Read<long>();
                DataSectionLength = Ar.Read<long>();
                EntriesCount = Ar.Read<int>();
                StructSize = Ar.Read<long>();
                ConstantsTableOffset = Ar.Read<long>();
                ConstantsTableEntriesCount = Ar.Read<int>();
                ConstantsSectionOffset = Ar.Read<long>();
                ConstantsSectionLength = Ar.Read<long>();
                break;
            case ERocoBinDataType.BinLocalize:
                ConstantsTableOffset = Ar.Read<long>();
                ConstantsTableEntriesCount = Ar.Read<int>();
                ConstantsSectionOffset = Ar.Read<long>();
                ConstantsSectionLength = Ar.Read<long>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}

public class FRocoSchema
{
    public required string Name;
    public required string Type;
    public required string UniqueKey;
    public required List<FRocoProperty> Properties;
}

public class FRocoProperty
{
    public required string Name;
    public required string Type;
    public required int Offset;
    public required int Size;
    public required int CompressedSize;
    public bool DynamicArray;
    public int? ArrayDim;
    public FRocoSchema? Struct;
}

public class FRocoBinData
{
    private static readonly uint _magic = 0x53DF17BE;
    private static readonly uint _binCompressedFooterLength = 68;
    private static readonly uint _binDataFooterLength = 56;
    private static readonly uint _binLocalizeFooterLength = 28;

    private readonly FRocoBinTable[] _dataTable = [];
    private readonly FRocoBinTable[] _constantsTable = [];

    public Dictionary<string, object?> RocoDataRows = [];
    public Dictionary<int, string> LocalizationStrings = [];

    public FRocoBinData(FArchive Ar, FRocoSchema schema, ERocoBinDataType type, FRocoBinData? locAr = null)
    {
        if (locAr == this)
            throw new ArgumentException("Localization source cannot be used on itself");
        if (Ar.Read<uint>() != _magic)
            throw new ParserException(Ar, $"Invalid magic, expected {_magic:X8}");

        Ar.Position = type switch
        {
            ERocoBinDataType.BinDataCompressed => Ar.Length - _binCompressedFooterLength,
            ERocoBinDataType.BinData => Ar.Length - _binDataFooterLength,
            ERocoBinDataType.BinLocalize => Ar.Length - _binLocalizeFooterLength,
            _ => throw new ParserException(Ar, $"Unknown footer length"),
        };

        var footer = new FRocoBinFooter(Ar, type);

        if (footer.DataTableOffset > 0)
        {
            Ar.Position = footer.DataTableOffset;
            _dataTable = Ar.ReadArray<FRocoBinTable>(footer.DataTableEntriesCount);
        }

        if (footer.ConstantsTableOffset > 0)
        {
            Ar.Position = footer.ConstantsTableOffset;
            _constantsTable = Ar.ReadArray<FRocoBinTable>(footer.ConstantsTableEntriesCount);
        }

        if (type is ERocoBinDataType.BinLocalize)
        {
            Ar.Position = footer.ConstantsSectionOffset;
            for (var i = 0; i < footer.ConstantsTableEntriesCount; i++)
            {
                var entry = _constantsTable[i];
                if (entry.Length == 0)
                    continue;

                Ar.Position = entry.Offset;
                var str = Encoding.UTF8.GetString(Ar.ReadBytes(entry.Length));
                LocalizationStrings[i + 1] = str;
            }

            return;
        }

        if (_dataTable.Length == 0)
            return;

        Ar.Position = footer.DataSectionOffset;
        for (var i = 0; i < footer.EntriesCount; i++)
        {
            var entry = _dataTable[i];
            if (entry.Length == 0)
                continue;
            if (entry.Offset != Ar.Position)
            {
                Log.Warning("Entry {0} offset mismatch, expected {1}, actual {2}", i, entry.Offset, Ar.Position);
                Ar.Position = entry.Offset;
            }

            var row = ParseStruct(Ar, schema, locAr);

            var idKey = schema.UniqueKey ?? "id";
            row.TryGetValue(idKey, out var id);

            string lookupKey = id?.ToString() ?? $"Unknown_{i}";
            RocoDataRows[lookupKey] = row;
        }
    }

    private string ReadRocoString(FArchive Ar)
    {
        var stringIndex = Ar.Read<uint>();

        if (stringIndex == 0)
            return string.Empty;

        var entry = _constantsTable[stringIndex - 1];

        var savedPos = Ar.Position;
        Ar.Position = entry.Offset;
        var str = Encoding.UTF8.GetString(Ar.ReadBytes(entry.Length));
        Ar.Position = savedPos;
        return str;
    }

    private object[] ReadArray(FArchive Ar, FRocoProperty prop, FRocoBinData? locAr = null, bool isDynamic = false)
    {
        int constantIndex = Ar.Read<int>();
        var constEntry = _constantsTable[constantIndex - 1];

        var savedPos = Ar.Position;
        Ar.Position = constEntry.Offset;

        var size = isDynamic ? prop.Size : 4;
        var count = constEntry.Length / size;
        var array = Ar.ReadArray(count, () => ReadRocoProperty(Ar, prop, locAr));
        Ar.Position = savedPos;
        return array;
    }

    private object ReadRocoProperty(FArchive Ar, FRocoProperty prop, FRocoBinData? locAr = null)
    {
        return prop.Type switch
        {
            "EUint32" => Ar.Read<uint>(),
            "EInt32" => Ar.Read<int>(),
            "EInt64" => Ar.Read<long>(),
            "EUint64" => Ar.Read<ulong>(),
            "EFloat" => Ar.Read<float>(),
            "EBool" => Ar.ReadByte() != 0,
            "EString" => ReadRocoString(Ar),
            "EStruct" => ParseNestedStruct(Ar, prop.Struct!, locAr),
            "ELocalizedString" => locAr?.LocalizationStrings[Ar.Read<int>()] ?? string.Empty, // Would be better to show all localizations at once but they haven't translated any so there's no need
            _ => throw new Exception($"Unknown type: {prop.Type}")
        };
    }

    private Dictionary<string, object?> ParseStruct(FArchive Ar, FRocoSchema schema, FRocoBinData? locAr = null)
    {
        var flagBytesCount = (int) Math.Ceiling(schema.Properties.Count / 8.0); // 1 byte = 8 bits
        var flags = Ar.ReadBytes(flagBytesCount);
        var row = new Dictionary<string, object?>();
        for (int j = 0; j < schema.Properties.Count; j++)
        {
            var prop = schema.Properties[j];

            // Check if the property is present based on the flags (bit 1 -> read, bit 0 -> skip, in Big Endian)
            bool isPresent = (flags[j / 8] & (1 << (7 - (j % 8)))) != 0;
            if (!isPresent) // or we can add null prop but I prefer to just skip
                continue;

            var isDynamicArray = prop.DynamicArray;
            if (isDynamicArray || prop.ArrayDim is not null)
            {
                row[prop.Name] = ReadArray(Ar, prop, locAr, isDynamicArray);
            }
            else
            {
                row[prop.Name] = ReadRocoProperty(Ar, prop, locAr);
            }
        }

        return row;
    }

    private Dictionary<string, object?> ParseNestedStruct(FArchive Ar, FRocoSchema schema, FRocoBinData? locAr = null)
    {
        var constEntryIndex = Ar.Read<int>();
        var constEntry = _constantsTable[constEntryIndex - 1];

        var savedPos = Ar.Position;
        Ar.Position = constEntry.Offset;
        var row = ParseStruct(Ar, schema, locAr);
        Ar.Position = savedPos;

        return row;
    }
}
