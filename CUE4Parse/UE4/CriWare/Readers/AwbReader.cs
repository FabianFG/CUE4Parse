using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.CriWare.Readers;

public struct Wave
{
    public int WaveId { get; set; }
    public long Offset { get; set; }
    public long Length { get; set; }
}

[JsonConverter(typeof(AwbReaderConverter))]
public sealed class AwbReader : IDisposable
{
    private readonly BinaryReader _binaryReader;
    private readonly long _offset;

    private readonly byte _offsetSize;
    private readonly ushort _waveIdAlignment;
    private readonly int _totalSubsongs;
    private readonly ushort _offsetAlignment;
    private readonly ushort _subkey;

    private readonly List<Wave> _waves;

    public AwbReader(Stream awbStream, bool isEmbedded) : this(awbStream, 0)
    {
        IsEmbedded = isEmbedded;
    }

    public AwbReader(Stream awbStream) : this(awbStream, 0) { }

    public AwbReader(Stream awbStream, long positionOffset)
    {
        _binaryReader = new BinaryReader(awbStream);
        _offset = positionOffset;

        _binaryReader.BaseStream.Position = _offset;

        if (!_binaryReader.ReadChars(4).SequenceEqual("AFS2"))
            throw new InvalidDataException("Incorrect magic.");

        _binaryReader.BaseStream.Position += 0x1;
        _offsetSize = _binaryReader.ReadByte();
        _waveIdAlignment = _binaryReader.ReadUInt16();
        _totalSubsongs = _binaryReader.ReadInt32();
        _offsetAlignment = _binaryReader.ReadUInt16();
        _subkey = _binaryReader.ReadUInt16();

        _waves = new List<Wave>(_totalSubsongs);

        for (int subsong = 1; subsong <= _totalSubsongs; subsong++)
        {
            long currentOffset = 0x10;

            long waveIdOffset = currentOffset + (subsong - 1) * _waveIdAlignment;

            _binaryReader.BaseStream.Position = _offset + waveIdOffset;

            int waveId = _binaryReader.ReadUInt16();

            currentOffset += _totalSubsongs * _waveIdAlignment;

            long subfileOffset = 0;
            long subfileNext = 0;
            long fileSize = _binaryReader.BaseStream.Length;

            currentOffset += (subsong - 1) * _offsetSize;

            _binaryReader.BaseStream.Position = _offset + currentOffset;

            switch (_offsetSize)
            {
                case 0x4:
                    subfileOffset = _binaryReader.ReadUInt32();
                    subfileNext = _binaryReader.ReadUInt32();
                    break;

                case 0x2:
                    subfileOffset = _binaryReader.ReadUInt16();
                    subfileNext = _binaryReader.ReadUInt16();
                    break;

                default:
                    Fail();
                    break;
            }

            subfileOffset += subfileOffset % _offsetAlignment > 0 ?
                _offsetAlignment - subfileOffset % _offsetAlignment : 0;
            subfileNext += subfileNext % _offsetAlignment > 0 && subfileNext < fileSize ?
                _offsetAlignment - subfileNext % _offsetAlignment : 0;
            long subfileSize = subfileNext - subfileOffset;

            _waves.Add(new Wave()
            {
                WaveId = waveId,
                Offset = subfileOffset,
                Length = subfileSize
            });
        }
    }

    public ushort Subkey => _subkey;

    public bool IsEmbedded { get; }

    public List<Wave> Waves => _waves;

    public Stream GetWaveSubfileStream(Wave wave)
    {
        return new SpliceStream(_binaryReader.BaseStream, _offset + wave.Offset, wave.Length);
    }

    private static void Fail()
    {
        throw new Exception("Failure reading AWB file.");
    }

    public void Dispose()
    {
        _binaryReader.Dispose();
    }
}
