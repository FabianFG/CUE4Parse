using System.IO;
using System.Text;

namespace CUE4Parse.UE4.Readers;

// Thank you RedHaze
public class FArchiveBigEndian : FArchive
{
    private FArchive _baseArchive;

    public FArchiveBigEndian(FArchive baseArchive)
    {
        _baseArchive = baseArchive;
        _baseArchive.ReverseBytes = true;
    }

    public override int Read(byte[] buffer, int offset, int count) => _baseArchive.Read(buffer, offset, count);
    public override string ReadString() => Encoding.UTF8.GetString(ReadArray<byte>());
    public override long Seek(long offset, SeekOrigin origin) => _baseArchive.Seek(offset, origin);

    public override bool CanSeek => _baseArchive.CanSeek;
    public override long Length => _baseArchive.Length;
    public override string Name => _baseArchive.Name;
    public override long Position
    {
        get => _baseArchive.Position;
        set => _baseArchive.Position = value;
    }

    public override object Clone() => new FArchiveBigEndian(_baseArchive);
}
