using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.CriWare.Readers;

[JsonConverter(typeof(AcbReaderConverter))]
public sealed class AcbReader : IDisposable
{
    private readonly Stream _outerStream;
    private readonly long _offset;
    private readonly uint _awbOffset;
    private readonly uint _awbLength;

    private readonly AcbParser _acbParser;

    public Dictionary<string, List<Dictionary<string, object?>>> AtomCueSheetData => _acbParser.TableData;

    public AcbReader(Stream acbStream) : this(acbStream, 0) { }

    public AcbReader(Stream acbStream, long positionOffset)
    {
        _outerStream = acbStream;
        _offset = positionOffset;

        acbStream.Position = _offset;

        UtfTable utfTable = new(acbStream, (uint)_offset, out int rows, out string name);

        if (rows != 1 || !name.Equals("Header"))
            throw new InvalidDataException("No Header table.");

        if (utfTable.Query(0, "AwbFile", out VLData awbValueData))
        {
            _awbOffset = awbValueData.Offset;
            _awbLength = awbValueData.Size;
        }
        else
        {
            _awbLength = 0;
        }

        HasMemoryAwb = _awbLength > 0;

        _outerStream.Position = positionOffset;
        _acbParser = new AcbParser(_outerStream);
    }

    public bool HasMemoryAwb { get; set; }

    public AwbReader? GetAwb()
    {
        if (_awbLength <= 0)
        {
            Log.Warning("Memory AWB length is 0, skipping");
            return null;
        }

        return new AwbReader(new SpliceStream(_outerStream, _awbOffset, _awbLength), true);
    }

    public T? TryGetTableValue<T>(string tableName, string key) where T : class
    {
        var value = TryGetTableValue(tableName, key);
        return value as T;
    }

    public object? TryGetTableValue(string tableName, string key)
    {
        if (AtomCueSheetData?.TryGetValue(tableName, out var list) != true || list == null)
            return null;

        foreach (var dict in list)
        {
            if (dict?.TryGetValue(key, out var value) == true)
                return value;
        }

        return null;
    }

    public string GetWaveName(int waveId, int port, bool memory)
    {
        _outerStream.Position = _offset;
        return _acbParser.LoadWaveName(waveId, port, memory);
    }

    public List<Waveform> GetWaveformsFromCueId(int cueId)
    {
        _outerStream.Position = _offset;
        return _acbParser.WaveformsFromCueId(cueId);
    }

    public void Dispose()
    {
        _outerStream.Dispose();
    }
}
