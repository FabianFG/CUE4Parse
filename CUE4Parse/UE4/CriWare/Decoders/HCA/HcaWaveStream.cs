using NAudio.Wave;
using System.IO;

namespace CUE4Parse.UE4.CriWare.Decoders.HCA;

public class HcaWaveStream : WaveStream
{
    private readonly Stream _hcaFileStream;
    private readonly BinaryReader _hcaFileReader;
    private readonly HcaDecoder _decoder;
    private readonly HcaInfo _info;
    private readonly long _dataStart;
    private readonly object _positionLock = new();

    private readonly short[][] _sampleBuffer;

    private long _samplePosition;

    public HcaWaveStream(Stream hcaFile, ulong key)
    {
        _hcaFileStream = hcaFile;
        _hcaFileReader = new(hcaFile);
        _decoder = new(hcaFile, key);
        _info = _decoder.HcaInfo;
        _dataStart = hcaFile.Position;

        _sampleBuffer = new short[_info.ChannelCount][];
        for (int i = 0; i < _info.ChannelCount; i++)
        {
            _sampleBuffer[i] = new short[_info.SamplesPerBlock];
        }

        Loop = false; // info.LoopEnabled; Don't enable, this will loop forever
        WaveFormat = new WaveFormat(_info.SamplingRate, _info.ChannelCount);

        _samplePosition = _info.EncoderDelay;
        FillBuffer(_samplePosition);
    }

    public HcaInfo Info => _info;

    public bool Loop { get; set; }

    public override WaveFormat WaveFormat { get; }

    public override long Length => _info.SampleCount * _info.ChannelCount * sizeof(short);

    public override long Position
    {
        get
        {
            lock (_positionLock)
            {
                return (_samplePosition - _info.EncoderDelay) * _info.ChannelCount * sizeof(short);
            }
        }
        set
        {
            lock (_positionLock)
            {
                _samplePosition = value / _info.ChannelCount / sizeof(short);
                _samplePosition += _info.EncoderDelay;

                if (Position < Length)
                    FillBuffer(_samplePosition);
            }
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        lock (_positionLock)
        {
            int read = 0;

            for (int i = 0; i < count / _info.ChannelCount / sizeof(short); i++)
            {
                if (_samplePosition - _info.EncoderDelay >= _info.LoopEndSample && Loop)
                {
                    _samplePosition = _info.LoopStartSample + _info.EncoderDelay;
                    FillBuffer(_samplePosition);
                }
                else if (Position >= Length)
                    break;

                if (_samplePosition % _info.SamplesPerBlock == 0)
                    FillBuffer(_samplePosition);

                for (int j = 0; j < _info.ChannelCount; j++)
                {
                    int bufferOffset = (i * _info.ChannelCount + j) * sizeof(short);
                    buffer[offset + bufferOffset] = (byte) _sampleBuffer[j][_samplePosition % _info.SamplesPerBlock];
                    buffer[offset + bufferOffset + 1] = (byte) (_sampleBuffer[j][_samplePosition % _info.SamplesPerBlock] >> 8);

                    read += sizeof(short);
                }

                _samplePosition++;
            }

            return read;
        }
    }

    private void FillBuffer(long sample)
    {
        int block = (int) (sample / _info.SamplesPerBlock);
        FillBuffer(block);
    }

    private void FillBuffer(int block)
    {
        if (block >= 0)
            _hcaFileStream.Position = _dataStart + block * _info.BlockSize;

        if (_hcaFileStream.Position < _hcaFileStream.Length)
        {
            byte[] blockBytes = _hcaFileReader.ReadBytes(_info.BlockSize);

            if (blockBytes.Length > 0)
            {
                _decoder.DecodeBlock(blockBytes);
                _decoder.ReadSamples16(_sampleBuffer);
            }
        }
    }
}
