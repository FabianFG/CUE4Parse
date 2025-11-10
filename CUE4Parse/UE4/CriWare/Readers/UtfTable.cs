using CUE4Parse.UE4.CriWare.Readers.Common;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace CUE4Parse.UE4.CriWare.Readers;

[Flags]
public enum ColumnFlag : byte
{
    Name = 0x10,
    Default = 0x20,
    Row = 0x40,
    Undefined = 0x80
}

public enum ColumnType : byte
{
    Byte = 0x00,
    SByte = 0x01,
    UInt16 = 0x02,
    Int16 = 0x03,
    UInt32 = 0x04,
    Int32 = 0x05,
    UInt64 = 0x06,
    Int64 = 0x07,
    Single = 0x08,
    Double = 0x09,
    String = 0x0A,
    VLData = 0x0B,
    UInt128 = 0x0C,
    Undefined = 0xFF
}

public struct Column
{
    public ColumnFlag Flag;
    public ColumnType Type;
    public string Name;
    public uint Offset;

    public override readonly string ToString()
    {
        return Name;
    }
}

public struct VLData
{
    public uint Offset;
    public uint Size;

    public override readonly string ToString()
    {
        return $"Offset: {Offset}, Length: {Size}";
    }
}

public struct Result
{
    public ColumnType Type;
    public object Value;
}

public sealed class UtfTable
{
    private readonly BinaryReaderEndian _binaryReader;
    private readonly uint _tableOffset;

    private readonly uint _tableSize;
    private readonly ushort _version;
    private readonly ushort _rowsOffset;
    private readonly uint _stringsOffset;
    private readonly uint _dataOffset;
    private readonly uint _nameOffset;

    private readonly ushort _columns;
    private readonly ushort _rowWidth;
    private readonly uint _rows;

    private readonly byte[] _schemaBuffer;
    private readonly Column[] _schema;

    private readonly uint _schemaOffset;
    private readonly uint _schemaSize;
    private readonly uint _rowsSize;
    private readonly uint _dataSize;
    private readonly uint _stringsSize;

    private readonly byte[] _stringTable;
    private readonly string _tableName;

    public UtfTable(Stream utfTableStream, out int utfTableRows, out string utfTableRowName) :
        this(utfTableStream, 0, out utfTableRows, out utfTableRowName)
    { }

    public UtfTable(Stream utfTableStream, uint offset) :
        this(utfTableStream, offset, out int _, out string _)
    { }

    public UtfTable(Stream utfTableStream, uint offset, out int utfTableRows, out string rowName)
    {
        _binaryReader = new BinaryReaderEndian(utfTableStream);
        _tableOffset = offset;

        _binaryReader.BaseStream.Position = offset;

        if (!_binaryReader.ReadChars(4).SequenceEqual("@UTF"))
            throw new InvalidDataException("Incorrect magic.");
        _tableSize = _binaryReader.ReadUInt32BE() + 0x08;
        _version = _binaryReader.ReadUInt16BE();
        _rowsOffset = (ushort)(_binaryReader.ReadUInt16BE() + 0x08);
        _stringsOffset = _binaryReader.ReadUInt32BE() + 0x08;
        _dataOffset = _binaryReader.ReadUInt32BE() + 0x08;
        _nameOffset = _binaryReader.ReadUInt32BE();
        _columns = _binaryReader.ReadUInt16BE();
        _rowWidth = _binaryReader.ReadUInt16BE();
        _rows = _binaryReader.ReadUInt32BE();

        _schemaOffset = 0x20;
        _schemaSize = _rowsOffset - _schemaOffset;
        _rowsSize = _stringsOffset - _rowsOffset;
        _stringsSize = _dataOffset - _stringsOffset;
        _dataSize = _tableSize - _dataOffset;

        if (_version != 0x00 && _version != 0x01)
            throw new InvalidDataException("Unknown @UTF version.");
        if (_tableOffset + _tableSize > utfTableStream.Length)
            throw new InvalidDataException("Table size exceeds bounds of file.");
        if (_rowsOffset > _tableSize || _stringsOffset > _tableSize || _dataOffset > _tableSize)
            throw new InvalidDataException("Offsets out of bounds.");
        if (_stringsSize <= 0 || _nameOffset > _stringsSize)
            throw new InvalidDataException("Invalid string table size.");
        if (Columns <= 0)
            throw new InvalidDataException("Table has no columns.");

        _schemaBuffer = new byte[_schemaSize];
        _binaryReader.BaseStream.Position = _tableOffset + _schemaOffset;
        if (_binaryReader.Read(_schemaBuffer, 0, (int)_schemaSize) != _schemaSize)
            throw new InvalidDataException("Failed to read schema.");

        _stringTable = new byte[_stringsSize];
        _binaryReader.BaseStream.Position = _tableOffset + _stringsOffset;
        if (_binaryReader.Read(_stringTable, 0, (int)_stringsSize) != _stringsSize)
            throw new InvalidDataException("Failed to read string table.");

        uint columnOffset = 0;
        uint schemaPos = 0;

        _tableName = GetStringFromTable(_nameOffset);

        _schema = new Column[_columns];

        BinaryReaderEndian bytesReader = new BinaryReaderEndian(new MemoryStream(_schemaBuffer) { Position = 0 });
        for (int i = 0; i < _columns; i++)
        {
            bytesReader.BaseStream.Position = schemaPos;

            byte info = bytesReader.ReadByte();
            uint nameOffset = bytesReader.ReadUInt32BE();

            if (nameOffset > _stringsSize)
                throw new InvalidDataException("String offset out of bounds.");
            schemaPos += 0x1 + 0x4;

            bytesReader.BaseStream.Position = schemaPos;

            _schema[i] = new Column()
            {
                Flag = (ColumnFlag)(info & 0xF0),
                Type = (ColumnType)(info & 0x0F),
                Name = "",
                Offset = 0
            };

            if (_schema[i].Flag == 0 ||
                !_schema[i].Flag.HasFlag(ColumnFlag.Name) ||
                 _schema[i].Flag.HasFlag(ColumnFlag.Default) && _schema[i].Flag.HasFlag(ColumnFlag.Row) ||
                 _schema[i].Flag.HasFlag(ColumnFlag.Undefined))
                throw new InvalidDataException("Unknown column flag combo found.");

            uint valueSize;
            switch (_schema[i].Type)
            {
                case ColumnType.Byte:
                case ColumnType.SByte:
                    valueSize = 0x1;
                    break;
                case ColumnType.UInt16:
                case ColumnType.Int16:
                    valueSize = 0x2;
                    break;
                case ColumnType.UInt32:
                case ColumnType.Int32:
                case ColumnType.Single:
                case ColumnType.String:
                    valueSize = 0x4;
                    break;
                case ColumnType.UInt64:
                case ColumnType.Int64:
                case ColumnType.VLData:
                    valueSize = 0x8;
                    break;
                default:
                    throw new InvalidDataException("Invalid column type.");
            }

            if (_schema[i].Flag.HasFlag(ColumnFlag.Name))
                _schema[i].Name = GetStringFromTable(nameOffset);

            if (_schema[i].Flag.HasFlag(ColumnFlag.Default))
            {
                _schema[i].Offset = schemaPos;
                schemaPos += valueSize;

                bytesReader.BaseStream.Position = schemaPos;

            }

            if (_schema[i].Flag.HasFlag(ColumnFlag.Row))
            {
                _schema[i].Offset = columnOffset;
                columnOffset += valueSize;
            }
        }

        utfTableRows = (int)_rows;
        rowName = GetStringFromTable(_nameOffset);

        bytesReader.Dispose();
    }

    public ushort Columns => _columns;

    public uint Rows => _rows;

    public Column[] Schema => _schema;

    public string TableName => _tableName;

    public Stream Stream => _binaryReader.BaseStream;

    public int GetColumn(string columnName)
    {
        for (int i = 0; i < _columns; i++)
        {
            Column column = _schema[i];

            if (column.Name is null || !column.Name.Equals(columnName))
                continue;

            return i;
        }

        return -1;
    }

    private bool Query(int row, int column, out Result result)
    {
        result = new Result();

        if (row >= _rows || row < 0)
            //throw new ArgumentOutOfRangeException(nameof(row));
            return false;
        if (column >= _columns || column < 0)
            //throw new ArgumentOutOfRangeException(nameof(column));
            return false;

        Column col = _schema[column];
        uint dataOffset = 0;
        BinaryReaderEndian? bytesReader = null;

        result.Type = col.Type;

        if (col.Flag.HasFlag(ColumnFlag.Default))
        {
            if (_schemaBuffer != null)
            {
                bytesReader = new BinaryReaderEndian(new MemoryStream(_schemaBuffer));
                bytesReader.BaseStream.Position = col.Offset;
            }
            else
            {
                dataOffset = _tableOffset + _schemaOffset + col.Offset;
            }
        }
        else if (col.Flag.HasFlag(ColumnFlag.Row))
        {
            dataOffset = (uint)(_tableOffset + _rowsOffset + row * _rowWidth + col.Offset);
        }
        else
            throw new InvalidDataException("Invalid flag.");

        _binaryReader.BaseStream.Position = dataOffset;

        switch (col.Type)
        {
            case ColumnType.Byte:
                result.Value = bytesReader != null ? bytesReader.ReadByte() : _binaryReader.ReadByte();
                break;
            case ColumnType.SByte:
                result.Value = bytesReader != null ? bytesReader.ReadSByte() : _binaryReader.ReadSByte();
                break;
            case ColumnType.UInt16:
                result.Value = bytesReader != null ? bytesReader.ReadUInt16BE() : _binaryReader.ReadUInt16BE();
                break;
            case ColumnType.Int16:
                result.Value = bytesReader != null ? bytesReader.ReadInt16BE() : _binaryReader.ReadInt16BE();
                break;
            case ColumnType.UInt32:
                result.Value = bytesReader != null ? bytesReader.ReadUInt32BE() : _binaryReader.ReadUInt32BE();
                break;
            case ColumnType.Int32:
                result.Value = bytesReader != null ? bytesReader.ReadInt32BE() : _binaryReader.ReadInt32BE();
                break;
            case ColumnType.UInt64:
                result.Value = bytesReader != null ? bytesReader.ReadUInt64BE() : _binaryReader.ReadUInt64BE();
                break;
            case ColumnType.Int64:
                result.Value = bytesReader != null ? bytesReader.ReadInt64BE() : _binaryReader.ReadInt64BE();
                break;
            case ColumnType.Single:
                result.Value = bytesReader != null ? bytesReader.ReadSingleBE() : _binaryReader.ReadSingleBE();
                break;
            //case ColumnType.Double:
            //    break;
            case ColumnType.String:
                uint nameOffset = bytesReader != null ? bytesReader.ReadUInt32BE() : _binaryReader.ReadUInt32BE();
                if (nameOffset > _stringsSize)
                    throw new InvalidDataException("Name offset out of bounds.");
                result.Value = GetStringFromTable(nameOffset);
                break;
            case ColumnType.VLData:
                if (bytesReader != null)
                {
                    result.Value = new VLData()
                    {
                        Offset = bytesReader.ReadUInt32BE(),
                        Size = bytesReader.ReadUInt32BE()
                    };
                }
                else
                {
                    result.Value = new VLData()
                    {
                        Offset = _binaryReader.ReadUInt32BE(),
                        Size = _binaryReader.ReadUInt32BE()
                    };
                }
                break;
            //case ColumnType.UInt128:
            //    break;
            default:
                return false;
        }

        return true;
    }

    public bool Query(int row, int column, Type type, out object value)
    {
        bool valid = Query(row, column, out Result result);
        bool enumParseResult = Enum.TryParse(type.Name, out ColumnType columnType);

        if (!valid || !enumParseResult || result.Type != columnType)
        {
            value = Activator.CreateInstance(type)!;
            return false;
        }

        value = result.Value;

        if (value is VLData vlData)
        {
            vlData.Offset += _tableOffset + _dataOffset;
            value = vlData;
        }

        return true;
    }

    public bool Query<T>(int row, int column, out T value)
    {
        bool valid = Query(row, column, typeof(T), out object outValue);
        value = (T)outValue;
        return valid;
    }

    public bool Query<T>(int row, string columnName, out T value) =>
        Query(row, GetColumn(columnName), out value);

    public bool Query(int row, int column, out uint offset, out uint size)
    {
        if (!Query(row, column, out VLData data))
        {
            offset = 0;
            size = 0;

            return false;
        }

        offset = data.Offset;
        size = data.Size;

        return true;
    }

    public bool Query(int row, string columnName, out uint offset, out uint size)
        => Query(row, GetColumn(columnName), out offset, out size);

    private string GetStringFromTable(uint offset)
    {
        if (offset >= _stringsSize)
            throw new InvalidDataException("Invalid string offset.");

        var eos = Array.IndexOf<byte>(_stringTable, 0, (int) offset);
        eos = eos == -1 ? _stringTable.Length : eos;
        return Encoding.UTF8.GetString(_stringTable.AsSpan()[(int) offset..eos]);
    }

    public UtfTable? OpenSubtable(string tableName)
    {
        if (!Query(0, tableName, out VLData tableValueData))
            throw new ArgumentException("Subtable does not exist.");

        if (tableValueData.Size < 1)
            return null;

        _binaryReader.BaseStream.Position = tableValueData.Offset;

        return new UtfTable(
            _binaryReader.BaseStream,
            tableValueData.Offset,
            out int _,
            out string _);
    }
}
