using System.Text;

namespace CUE4Parse.GameTypes.FantasyLifeTheGirlWhoStealsTime.Encryption;

// Credits to DRayX for figuring out the decryption algorithm:
// https://gist.github.com/DRayX/38515add7c9a754ef337e3095e6f28fe
internal sealed class FantasyLifeAwbStream : Stream
{
    private const uint CrcPolynomial = 0xEDB88320;
    private static readonly uint[] _crcTable = CreateCrcTable();

    private readonly Stream _innerStream;
    private readonly uint _seed;
    private readonly bool _leaveOpen;

    public FantasyLifeAwbStream(Stream innerStream, string awbName, bool leaveOpen)
    {
        _innerStream = innerStream;
        _seed = ComputeCrc32(Encoding.ASCII.GetBytes(Path.GetFileName(awbName)), 0);
        _leaveOpen = leaveOpen;
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => false;
    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var position = Position;
        var bytesRead = _innerStream.Read(buffer, offset, count);
        Decrypt(buffer.AsSpan(offset, bytesRead), position);
        return bytesRead;
    }

    public override int Read(Span<byte> buffer)
    {
        var position = Position;
        var bytesRead = _innerStream.Read(buffer);
        Decrypt(buffer[..bytesRead], position);
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
    public override void Flush() => _innerStream.Flush();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_leaveOpen)
            _innerStream.Dispose();
        base.Dispose(disposing);
    }

    private void Decrypt(Span<byte> buffer, long start)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            var position = start + i;
            var crc = ComputeSeededCrc32(_seed, unchecked((uint) position));
            var shift = (int) (position % 4 * 2);
            buffer[i] ^= (byte) (
                (crc >> shift & 0x03) |
                (crc >> (shift + 6) & 0x0C) |
                (crc >> (shift + 12) & 0x30) |
                (crc >> (shift + 18) & 0xC0));
        }
    }

    private static uint ComputeCrc32(ReadOnlySpan<byte> data, uint initialValue)
    {
        var crc = initialValue ^ uint.MaxValue;
        foreach (var value in data)
        {
            crc = _crcTable[(crc ^ value) & 0xFF] ^ crc >> 8;
        }

        return crc ^ uint.MaxValue;
    }

    private static uint ComputeSeededCrc32(uint seed, uint initialValue)
    {
        var crc = initialValue ^ uint.MaxValue;
        crc = _crcTable[(crc ^ (byte) seed) & 0xFF] ^ crc >> 8;
        crc = _crcTable[(crc ^ (byte) (seed >> 8)) & 0xFF] ^ crc >> 8;
        crc = _crcTable[(crc ^ (byte) (seed >> 16)) & 0xFF] ^ crc >> 8;
        crc = _crcTable[(crc ^ (byte) (seed >> 24)) & 0xFF] ^ crc >> 8;
        return crc ^ uint.MaxValue;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            var value = i;
            for (var bit = 0; bit < 8; bit++)
            {
                value = (value >> 1) ^ ((value & 1) * CrcPolynomial);
            }
            table[i] = value;
        }

        return table;
    }
}
