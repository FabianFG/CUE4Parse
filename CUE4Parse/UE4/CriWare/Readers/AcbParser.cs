using CUE4Parse.UE4.CriWare.Readers.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace CUE4Parse.UE4.CriWare.Readers;

struct CueName
{
    public ushort CueIndex;
    public string Name;
}

struct Cue
{
    public uint Id;
    public EReferenceType ReferenceType;
    public ushort ReferenceIndex;
}

struct BlockSequence
{
    public ushort NumTracks;
    public uint TrackIndexOffset;
    public uint TrackIndexSize;
    public ushort NumBlocks;
    public uint BlockIndexOffset;
    public uint BlockIndexSize;
}

struct Block
{
    public ushort NumTracks;
    public uint TrackIndexOffset;
    public uint TrackIndexSize;
    public ushort ActionTrackStartIndex;
    public ushort NumActionTracks;
}

struct Sequence
{
    public ushort NumTracks;
    public uint TrackIndexOffset;
    public uint TrackIndexSize;
    public ESequenceType Type;
    public ushort ActionTrackStartIndex;
    public ushort NumActionTracks;
}

struct ActionTrack
{
    public ushort EventIndex;
    public ushort CommandIndex;
    public ETargetType TargetType;
    public string TargetName;
    public uint TargetId;
    public string TargetAcbName;
    public byte Scope;
    public ushort TargetTrackNo;
}

struct Track
{
    public ushort EventIndex;
}

struct TrackCommand
{
    public uint CommandOffset;
    public uint CommandSize;
}

struct Synth
{
    public byte Type;
    public uint ReferenceItemsOffset;
    public uint ReferenceItemsSize;
    public ushort ActionTrackStartIndex;
    public ushort NumActionTracks;
}

public struct Waveform
{
    public ushort Id;
    public ushort StreamId;
    public ushort PortNo;
    public EWaveformStreamType Streaming;
    public EEncodeType EncodeType;
}

public enum EEncodeType : byte
{
    ADX = 0,
    HCA = 2,
    HCA_ALT = 6,
    VAG = 7,
    ATRAC3 = 8,
    BCWAV = 9,
    ATRAC9 = 11,
    DSP = 13,
    None = 255
}

public enum EWaveformStreamType : byte
{
    Memory = 0,
    Streaming = 1,
    Both = 2
}

public enum EReferenceType : byte
{
    Waveform = 1,
    Synth = 2,
    Sequence = 3,
    BlockSequence = 8,
    Nothing = 255
}

public enum ETargetType : byte
{
    AnyAcb = 0,
    SpecificAcb = 1
}

public enum ESequenceType : byte
{
    Polyphonic = 0,
    Sequential = 1,
    Shuffle = 2,
    Random = 3,
    RandomNoRepeat = 4,
    Switch = 5,
    ComboSequential = 6
}

public class AcbParser
{
    public readonly Dictionary<string, List<Dictionary<string, object?>>> TableData = [];

    private readonly Stream _acbStream;

    private readonly UtfTable _header;
    private UtfTable? _cueNames;

    private BinaryReaderEndian? _cueReader;
    private BinaryReaderEndian? _cueNameReader;
    private BinaryReaderEndian? _blockSequenceReader;
    private BinaryReaderEndian? _blockReader;
    private BinaryReaderEndian? _sequenceReader;
    private BinaryReaderEndian? _actionTrackReader;
    private BinaryReaderEndian? _trackReader;
    private BinaryReaderEndian? _trackCommandReader;
    private BinaryReaderEndian? _synthReader;
    private BinaryReaderEndian? _waveformReader;

    private Cue[] _cue = [];
    private CueName[] _cueName = [];
    private BlockSequence[] _blockSequence = [];
    private Block[] _block = [];
    private Sequence[] _sequence = [];
    private ActionTrack[] _actionTrack = [];
    private Track[] _track = [];
    private TrackCommand[] _trackCommand = [];
    private Synth[] _synth = [];
    private Waveform[] _waveform = [];

    private int _cueRows;
    private int _cueNameRows;
    private int _blockSequenceRows;
    private int _blockRows;
    private int _sequenceRows;
    private int _actionTrackRows;
    private int _trackRows;
    private int _trackCommandRows;
    private int _synthRows;
    private int _waveFormRows;

    private bool _isMemory;
    private int _targetWaveId;
    private int _targetPort;
    private int _targetCueId;

    private int _synthDepth;
    private int _sequenceDepth;

    private short _cueNameIndex;
    private string _cueNameName = string.Empty;
    private int _awbNameCount;
    private readonly List<short> _awbNameList = [];
    private string _name = string.Empty;

    private uint _currentCueId;
    private readonly List<Waveform> _waveformsFromCueId = [];
    private bool _cueOnly;

    public AcbParser(Stream acb)
    {
        _acbStream = acb;
        _header = new UtfTable(acb, (uint) _acbStream.Position);
        StoreAllUtfTableRows();
    }

    private void StoreAllUtfTableRows()
    {
        foreach (var col in _header.Schema)
        {
            if (col.Type != ColumnType.VLData)
                continue;

            UtfTable? sub = null;
            try
            { sub = _header.OpenSubtable(col.Name); }
            catch { }

            if (sub == null || sub.Rows == 0)
                continue;

            var data = new List<Dictionary<string, object?>>();
            for (int i = 0; i < sub.Rows; i++)
            {
                var rowDict = new Dictionary<string, object?>();
                foreach (var fieldCol in sub.Schema)
                {
                    object? val = null;
                    switch (fieldCol.Type)
                    {
                        case ColumnType.Byte:
                            sub.Query(i, fieldCol.Name, out byte bval);
                            val = bval;
                            break;
                        case ColumnType.SByte:
                            sub.Query(i, fieldCol.Name, out sbyte sbval);
                            val = sbval;
                            break;
                        case ColumnType.UInt16:
                            sub.Query(i, fieldCol.Name, out ushort usval);
                            val = usval == 0xFFFF ? -1 : usval;
                            break;
                        case ColumnType.Int16:
                            sub.Query(i, fieldCol.Name, out short sval);
                            val = sval;
                            break;
                        case ColumnType.UInt32:
                            sub.Query(i, fieldCol.Name, out uint uval);
                            val = uval == 0xFFFFFFFF ? -1 : uval;
                            break;
                        case ColumnType.Int32:
                            sub.Query(i, fieldCol.Name, out int ival);
                            val = ival;
                            break;
                        case ColumnType.UInt64:
                            sub.Query(i, fieldCol.Name, out ulong ulval);
                            val = ulval;
                            break;
                        case ColumnType.Int64:
                            sub.Query(i, fieldCol.Name, out long ilval);
                            val = ilval;
                            break;
                        case ColumnType.Single:
                            sub.Query(i, fieldCol.Name, out float fval);
                            val = fval;
                            break;
                        case ColumnType.String:
                            sub.Query(i, fieldCol.Name, out string str);
                            val = str;
                            break;
                        case ColumnType.VLData:
                            if (sub.Query(i, fieldCol.Name, out VLData vldata) && vldata.Size > 0)
                            {
                                long prevPos = sub.Stream.Position;
                                try
                                {
                                    sub.Stream.Position = vldata.Offset;
                                    byte[] dataBytes = new byte[vldata.Size];
                                    int read = sub.Stream.Read(dataBytes, 0, (int) vldata.Size);
                                    val = (read == vldata.Size) ? dataBytes : [];
                                }
                                catch { val = Array.Empty<byte>(); }
                                finally { sub.Stream.Position = prevPos; }
                            }
                            else
                            {
                                val = Array.Empty<byte>();
                            }
                            break;
                        default:
                            val = null;
                            break;
                    }
                    rowDict[fieldCol.Name] = val;
                }

                data.Add(rowDict);
            }

            TableData[sub.TableName] = data;
        }
    }

    bool OpenUtfSubtable(out BinaryReaderEndian tableReader, out UtfTable table, string tableName, out int rows)
    {
        if (!_header.Query(0, tableName, out VLData data))
            throw new ArgumentException("Error reading table.");

        tableReader = new BinaryReaderEndian(_acbStream);
        table = new UtfTable(tableReader.BaseStream, data.Offset, out rows, out string _);

        return true;
    }

    void AddAcbName(EWaveformStreamType streaming)
    {
        if (_cueNameName.Length == 0)
            return;

        for (int i = 0; i < _awbNameCount; i++)
        {
            if (_awbNameList[i] == _cueNameIndex)
                return;
        }

        if (_awbNameCount > 0)
        {
            _name += "; ";
            _name += _cueNameName;
        }
        else
            _name = _cueNameName;

        if (streaming is EWaveformStreamType.Both && _isMemory)
            _name += " [pre]";

        _awbNameList.Add(_cueNameIndex);
        _awbNameCount++;
    }

    void PreloadAcbWaveForm()
    {
        ref int rows = ref _waveFormRows;

        if (rows != 0)
            return;
        if (!OpenUtfSubtable(out _waveformReader, out UtfTable table, "WaveformTable", out rows))
            throw new Exception("Failure opening Waveform table.");
        if (rows == 0)
            return;

        _waveform = new Waveform[rows];

        int cId = table.GetColumn("Id");
        int cMemoryAwbId = table.GetColumn("MemoryAwbId");
        int cStreamAwbId = table.GetColumn("StreamAwbId");
        int cStreamAwbPortNo = table.GetColumn("StreamAwbPortNo");
        int cStreaming = table.GetColumn("Streaming");
        int cEncodeType = table.GetColumn("EncodeType");

        for (int i = 0; i < rows; i++)
        {
            ref Waveform r = ref _waveform[i];

            table.Query(i, cMemoryAwbId, out r.Id);
            table.Query(i, cStreamAwbId, out r.StreamId);
            table.Query(i, cStreamAwbPortNo, out r.PortNo);
            table.Query(i, cStreaming, out byte streaming);
            table.Query(i, cEncodeType, out byte encodeType);
            r.Streaming = (EWaveformStreamType) streaming;
            r.EncodeType = (EEncodeType) encodeType;
        }
    }

    void LoadAcbWaveForm(ushort index)
    {
        PreloadAcbWaveForm();

        if (index > _waveFormRows)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_waveform is null)
            return;

        ref Waveform r = ref _waveform[index];

        if (_currentCueId == _targetCueId)
        {
            _waveformsFromCueId.Add(r);
        }

        if (_cueOnly)
            return;

        if (r.Id != _targetWaveId)
            return;

        if (_targetPort >= 0 && r.PortNo != 0xFFFF && r.PortNo != _targetPort)
            return;

        if ((_isMemory && r.Streaming is EWaveformStreamType.Streaming) || (!_isMemory && r.Streaming is EWaveformStreamType.Memory))
            return;

        AddAcbName(r.Streaming);

        return;
    }

    void PreloadAcbSynth()
    {
        ref int rows = ref _synthRows;

        if (rows != 0)
            return;
        if (!OpenUtfSubtable(out _synthReader, out UtfTable table, "SynthTable", out rows))
            throw new Exception("Failure opening Synth table.");
        if (rows == 0)
            return;

        _synth = new Synth[rows];

        int cType = table.GetColumn("Type");
        int cReferenceItems = table.GetColumn("ReferenceItems");
        int cActionTrackStartIndex = table.GetColumn("ActionTrackStartIndex");
        int cNumActionTracks = table.GetColumn("NumActionTracks");

        for (int i = 0; i < rows; i++)
        {
            ref Synth r = ref _synth[i];

            table.Query(i, cType, out r.Type);
            table.Query(i, cReferenceItems, out r.ReferenceItemsOffset, out r.ReferenceItemsSize);
            table.Query(i, cActionTrackStartIndex, out r.ActionTrackStartIndex);
            table.Query(i, cNumActionTracks, out r.NumActionTracks);
        }
    }

    void LoadAcbSynth(ushort index)
    {
        PreloadAcbSynth();

        if (index > _synthRows)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_synth is null)
            return;
        if (_synthReader is null)
            return;

        ref Synth r = ref _synth[index];

        _synthDepth++;

        if (_synthDepth > 3)
            throw new Exception("Synth depth too high");

        int count = (int) (r.ReferenceItemsSize / 4);
        for (int i = 0; i < count; i++)
        {
            _synthReader.BaseStream.Position = r.ReferenceItemsOffset + i * 4;

            ushort itemType = _synthReader.ReadUInt16BE();
            ushort itemIndex = _synthReader.ReadUInt16BE();

            switch (itemType)
            {
                case 0:
                    count = 0;
                    break;

                case 1:
                    LoadAcbWaveForm(itemIndex);
                    break;

                case 2:
                    LoadAcbSynth(itemIndex);
                    break;

                case 3:
                    LoadAcbSequence(itemIndex);
                    break;

                case 6:
                default:
                    count = 0;
                    break;
            }
        }

        _synthDepth--;
    }

    void LoadAcbCommandTlvs(BinaryReaderEndian reader, uint commandOffset, uint commandSize)
    {
        uint pos = 0;
        uint maxPos = commandSize;

        while (pos < maxPos)
        {
            reader.BaseStream.Position = commandOffset + pos;

            ushort tlvCode = reader.ReadUInt16BE();
            ushort tlvSize = reader.ReadByte();

            pos += 3;

            switch (tlvCode)
            {
                case 2000:
                case 2003:
                    if (tlvSize < 4)
                        break;

                    reader.BaseStream.Position = commandOffset + pos;

                    ushort tlvType = reader.ReadUInt16BE();
                    ushort tlvIndex = reader.ReadUInt16BE();

                    switch (tlvType)
                    {
                        case 2:
                            LoadAcbSynth(tlvIndex);
                            break;

                        case 3:
                            LoadAcbSequence(tlvIndex);
                            break;

                        default:
                            maxPos = 0;
                            break;
                    }

                    break;

                default:
                    break;
            }

            pos += tlvSize;
        }
    }

    void PreloadAcbTrackCommand()
    {
        ref int rows = ref _trackCommandRows;

        if (rows != 0)
            return;
        if (!OpenUtfSubtable(out _trackCommandReader, out UtfTable table, "TrackEventTable", out rows))
        {
            if (!OpenUtfSubtable(out _trackCommandReader, out table, "CommandTable", out rows))
                throw new Exception("Failure opening Command table.");
        }
        if (rows == 0)
            return;

        _trackCommand = new TrackCommand[rows];

        int cCommand = table.GetColumn("Command");

        for (int i = 0; i < rows; i++)
        {
            ref TrackCommand r = ref _trackCommand[i];

            table.Query(i, cCommand, out r.CommandOffset, out r.CommandSize);
        }
    }

    void LoadAcbTrackCommand(ushort index)
    {
        PreloadAcbTrackCommand();

        if (index > _trackCommandRows)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_trackCommandReader is null)
            return;
        if (_trackCommand is null)
            return;

        LoadAcbCommandTlvs(
            _trackCommandReader,
            _trackCommand[index].CommandOffset,
            _trackCommand[index].CommandSize);
    }

    void PreloadAcbActionTrack()
    {
        ref int rows = ref _actionTrackRows;

        if (rows != 0)
            return;
        if (!OpenUtfSubtable(out _actionTrackReader, out UtfTable table, "ActionTrackTable", out rows))
            return;
        if (rows == 0)
            return;

        _actionTrack = new ActionTrack[rows];

        int cEventIndex = table.GetColumn("EventIndex");
        int cCommandIndex = table.GetColumn("CommandIndex");
        int cTargetType = table.GetColumn("TargetType");
        int cTargetName = table.GetColumn("TargetName");
        int cTargetId = table.GetColumn("TargetId");
        int cTargetAcbName = table.GetColumn("TargetAcbName");
        int cScope = table.GetColumn("Scope");
        int cTargetTrackNo = table.GetColumn("TargetTrackNo");

        for (int i = 0; i < rows; i++)
        {
            ref ActionTrack r = ref _actionTrack[i];

            table.Query(i, cEventIndex, out r.EventIndex);
            table.Query(i, cCommandIndex, out r.CommandIndex);
            table.Query(i, cTargetType, out byte targetType);
            r.TargetType = (ETargetType) targetType;
            table.Query(i, cTargetName, out r.TargetName);
            table.Query(i, cTargetId, out r.TargetId);
            table.Query(i, cTargetAcbName, out r.TargetAcbName);
            table.Query(i, cScope, out r.Scope);
            table.Query(i, cTargetTrackNo, out r.TargetTrackNo);
        }
    }

    void LoadAcbActionTrack(ushort index)
    {
        PreloadAcbActionTrack();

        if (index >= _actionTrackRows)
            return;

        if (_actionTrack is null)
            return;

        ref ActionTrack r = ref _actionTrack[index];

        if (r.EventIndex != 0xFFFF)
        {
            LoadAcbTrackCommand(r.EventIndex);
        }

        if (r.CommandIndex != 0xFFFF)
        {
            LoadAcbTrackCommand(r.CommandIndex);
        }
    }

    void PreloadAcbTrack()
    {
        ref int rows = ref _trackRows;

        if (rows != 0)
            return;
        if (!OpenUtfSubtable(out _trackReader, out UtfTable table, "TrackTable", out rows))
            throw new Exception("Failure opening Track table.");
        if (rows == 0)
            return;

        _track = new Track[rows];

        int cEventIndex = table.GetColumn("EventIndex");

        for (int i = 0; i < rows; i++)
        {
            ref Track r = ref _track[i];

            table.Query(i, cEventIndex, out r.EventIndex);
        }
    }

    void LoadAcbTrack(ushort index)
    {
        PreloadAcbTrack();

        if (index > _trackRows)
            return;
            //throw new ArgumentOutOfRangeException(nameof(index));

        if (_track is null)
            return;

        ref Track r = ref _track[index];

        if (r.EventIndex == 65535)
            return;

        LoadAcbTrackCommand(r.EventIndex);
    }

    void PreloadAcbSequence()
    {
        ref int rows = ref _sequenceRows;

        if (rows != 0)
            return;
        if (!OpenUtfSubtable(out _sequenceReader, out UtfTable table, "SequenceTable", out rows))
            throw new Exception("Failure opening Sequence table.");
        if (rows == 0)
            return;

        _sequence = new Sequence[rows];

        int cNumTracks = table.GetColumn("NumTracks");
        int cTrackIndex = table.GetColumn("TrackIndex");
        int cType = table.GetColumn("Type");
        int cActionTrackStartIndex = table.GetColumn("ActionTrackStartIndex");
        int cNumActionTracks = table.GetColumn("NumActionTracks");

        for (int i = 0; i < rows; i++)
        {
            ref Sequence r = ref _sequence[i];

            table.Query(i, cNumTracks, out r.NumTracks);
            table.Query(i, cTrackIndex, out r.TrackIndexOffset, out r.TrackIndexSize);
            table.Query(i, cType, out byte type);
            r.Type = (ESequenceType) type;
            table.Query(i, cActionTrackStartIndex, out r.ActionTrackStartIndex);
            table.Query(i, cNumActionTracks, out r.NumActionTracks);
        }
    }

    void LoadAcbSequence(uint index)
    {
        PreloadAcbSequence();

        if (index > _sequenceRows)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_sequence is null)
            return;
        if (_sequenceReader is null)
            return;

        ref Sequence r = ref _sequence[index];

        _sequenceDepth++;

        if (_sequenceDepth > 3)
            throw new Exception("Sequence depth too high.");

        if (r.NumTracks * 2 > r.TrackIndexSize)
            throw new Exception("Wrong Sequence.TrackIndex size.");

        switch (r.Type)
        {
            default:
                for (int i = 0; i < r.NumTracks; i++)
                {
                    _sequenceReader.BaseStream.Position = r.TrackIndexOffset + i * 2;

                    short trackIndexIndex = _sequenceReader.ReadInt16BE();
                    LoadAcbTrack((ushort) trackIndexIndex);
                }
                break;
        }

        _sequenceDepth--;
    }

    void PreloadAcbBlock()
    {
        ref int rows = ref _blockRows;

        if (rows != 0)
            return;
        if (!OpenUtfSubtable(out _blockReader, out UtfTable table, "BlockTable", out rows))
            throw new Exception("Failure opening Block table.");
        if (rows == 0)
            return;

        _block = new Block[rows];

        int cNumTracks = table.GetColumn("NumTracks");
        int cTrackIndex = table.GetColumn("TrackIndex");
        int cActionTrackStartIndex = table.GetColumn("ActionTrackStartIndex");
        int cNumActionTracks = table.GetColumn("NumActionTracks");

        for (int i = 0; i < rows; i++)
        {
            ref Block r = ref _block[i];
            table.Query(i, cNumTracks, out r.NumTracks);
            table.Query(i, cTrackIndex, out VLData data);
            r.TrackIndexOffset = data.Offset;
            r.TrackIndexSize = data.Size;
            table.Query(i, cActionTrackStartIndex, out r.ActionTrackStartIndex);
            table.Query(i, cNumActionTracks, out r.NumActionTracks);
        }
    }

    void LoadAcbBlock(ushort index)
    {
        PreloadAcbBlock();

        if (index > _blockRows)
            return;
            //throw new ArgumentOutOfRangeException(nameof(index));

        if (_block is null)
            return;
        if (_blockReader is null)
            return;

        ref Block r = ref _block[index];

        if (r.NumTracks * 2 > r.TrackIndexSize)
            throw new Exception("Wrong Block.TrackIndex size.");

        for (int i = 0; i < r.NumTracks; i++)
        {
            _blockReader.BaseStream.Position = r.TrackIndexOffset + i * 2;

            short trackIndexIndex = _blockReader.ReadInt16BE();
            LoadAcbTrack((ushort) trackIndexIndex);
        }
    }

    void PreloadAcbBlockSequence()
    {
        ref int rows = ref _blockSequenceRows;

        if (rows != 0)
            return;
        if (!OpenUtfSubtable(out _blockSequenceReader, out UtfTable table, "BlockSequenceTable", out rows))
            throw new Exception("Failure opening BlockSequence table.");
        if (rows == 0)
            return;

        _blockSequence = new BlockSequence[rows];

        int cNumTracks = table.GetColumn("NumTracks");
        int cTrackIndex = table.GetColumn("TrackIndex");
        int cNumBlocks = table.GetColumn("NumBlocks");
        int cBlockIndex = table.GetColumn("BlockIndex");

        for (int i = 0; i < rows; i++)
        {
            ref BlockSequence r = ref _blockSequence[i];

            table.Query(i, cNumTracks, out r.NumTracks);
            table.Query(i, cTrackIndex, out r.TrackIndexOffset, out r.TrackIndexSize);
            table.Query(i, cNumBlocks, out r.NumBlocks);
            table.Query(i, cBlockIndex, out r.BlockIndexOffset, out r.BlockIndexSize);
        }
    }

    void LoadAcbBlockSequence(ushort index)
    {
        PreloadAcbBlockSequence();

        if (index > _blockSequenceRows)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_blockSequence is null)
            return;
        if (_blockSequenceReader is null)
            return;

        ref BlockSequence r = ref _blockSequence[index];

        if (r.NumTracks * 2 > r.TrackIndexSize)
            throw new Exception("Wrong BlockSequence.TrackIndex size.");

        for (int i = 0; i < r.NumTracks; i++)
        {
            _blockSequenceReader.BaseStream.Position = r.TrackIndexOffset + i * 2;

            short trackIndexIndex = _blockSequenceReader.ReadInt16();
            LoadAcbTrack((ushort) trackIndexIndex);
        }

        if (r.NumBlocks * 2 > r.BlockIndexSize)
            throw new Exception("Wrong BlockSequence.BlockIndex size.");

        for (int i = 0; i < r.NumBlocks; i++)
        {
            _blockSequenceReader.BaseStream.Position = r.BlockIndexOffset + i * 2;

            short blockIndexIndex = _blockSequenceReader.ReadInt16();
            LoadAcbBlock((ushort) blockIndexIndex);
        }
    }

    void PreloadAcbCue()
    {
        ref int rows = ref _cueRows;

        if (rows != 0)
            return;
        if (!OpenUtfSubtable(out _cueReader, out UtfTable table, "CueTable", out rows))
            throw new Exception("Failure opening Cue table.");
        if (rows == 0)
            return;

        _cue = new Cue[rows];

        int cCueId = table.GetColumn("CueId");
        int cReferenceType = table.GetColumn("ReferenceType");
        int cReferenceIndex = table.GetColumn("ReferenceIndex");

        for (int i = 0; i < rows; i++)
        {
            ref Cue r = ref _cue[i];

            table.Query(i, cCueId, out r.Id);
            table.Query(i, cReferenceType, out byte referenceType);
            r.ReferenceType = (EReferenceType) referenceType;
            table.Query(i, cReferenceIndex, out r.ReferenceIndex);
        }
    }

    void LoadAcbCue(ushort index)
    {
        PreloadAcbCue();

        if (index > _cueRows)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_cue is null)
            return;

        ref Cue r = ref _cue[index];

        _currentCueId = r.Id;

        switch (r.ReferenceType)
        {
            case EReferenceType.Waveform:
                LoadAcbWaveForm(r.ReferenceIndex);
                break;

            case EReferenceType.Synth:
                LoadAcbSynth(r.ReferenceIndex);
                break;

            case EReferenceType.Sequence:
                LoadAcbSequence(r.ReferenceIndex);
                break;

            case EReferenceType.BlockSequence:
                LoadAcbBlockSequence(r.ReferenceIndex);
                break;

            default:
                break;
        }
    }

    void PreloadAcbCueName()
    {
        ref UtfTable? table = ref _cueNames;
        ref int rows = ref _cueNameRows;

        if (rows != 0)
            return;
        if (!OpenUtfSubtable(out _cueNameReader, out table, "CueNameTable", out rows))
            throw new Exception("Failure opening CueName table.");
        if (rows == 0)
            return;

        _cueName = new CueName[rows];

        int cCueIndex = table.GetColumn("CueIndex");
        int cCueName = table.GetColumn("CueName");

        for (int i = 0; i < rows; i++)
        {
            ref CueName r = ref _cueName[i];

            table.Query(i, cCueIndex, out r.CueIndex);
            table.Query(i, cCueName, out string name);
            r.Name = name ?? "";
        }
    }

    void LoadAcbCueName(ushort index)
    {
        PreloadAcbCueName();

        if (index > _cueNameRows)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_cueName is null)
            return;

        ref CueName r = ref _cueName[index];

        _cueNameIndex = (short) index;
        _cueNameName = r.Name;

        LoadAcbCue(r.CueIndex);
    }

    public string LoadWaveName(int waveId, int port, bool memory)
    {
        _targetWaveId = waveId;
        _targetPort = port;
        _isMemory = memory;

        _name = "";
        _awbNameCount = 0;

        PreloadAcbCueName();
        for (ushort i = 0; i < _cueNameRows; i++)
        {
            LoadAcbCueName(i);
        }

        return _name;
    }

    public List<Waveform> WaveformsFromCueId(int cueId)
    {
        _targetCueId = cueId;
        _waveformsFromCueId.Clear();
        _cueOnly = true;

        PreloadAcbCue();
        for (ushort i = 0; i < _cueRows; i++)
        {
            LoadAcbCue(i);
        }

        _cueOnly = false;

        return _waveformsFromCueId;
    }
}
