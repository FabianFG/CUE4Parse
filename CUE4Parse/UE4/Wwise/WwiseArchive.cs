using System.Text;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Wwise;

public sealed class FWwiseArchive(FArchive Ar) : FArchive(Ar.Versions)
{
    /// <summary>
    /// Wwise version, read from the BankHeader section of the .bnk file
    /// Can also be deducted from plugin version
    /// </summary>
    public uint Version;

    /// <summary>
    /// Read from BankHeader section of the .bnk file
    /// Only relevant for versions <= 126
    /// </summary>
    public bool HasFeedback;

    public FWwiseArchive(string name, byte[] data, VersionContainer? versions = null) : this(new FByteArchive(name, data, versions)) { }

    public override int Read(byte[] buffer, int offset, int count) => Ar.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => Ar.Seek(offset, origin);
    public override void SetLength(long value) => Ar.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => Ar.Write(buffer, offset, count);
    public override void Flush() => Ar.Flush();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            Ar.Dispose();
    }

    public override string Name => Ar.Name;
    public override long Length => Ar.Length;
    public override bool CanSeek => Ar.CanSeek;
    public override long Position
    {
        get => Ar.Position;
        set => Ar.Position = value;
    }

    public override object Clone() => new FWwiseArchive(Ar) { Version = Version };

    public bool IsSupported() => WwiseVersionInfo.IsSupported(Version);

    public string ReadStzString()
    {
        var bytes = new List<byte>(16);
        while (true)
        {
            var b = Read<byte>();
            if (b == 0)
                break;
            bytes.Add(b);

            if (bytes.Count >= 255)
                throw new ArgumentException("ReadStz: string too long (no terminator within 255 bytes).");
        }

        return Encoding.UTF8.GetString([.. bytes]);
    }

    public int Read7BitEncodedIntBE()
    {
        int max = 0;

        byte cur = Read<byte>();
        int value = cur & 0x7F;

        while ((cur & 0x80) != 0)
        {
            if (++max >= 10)
                throw new FormatException("Unexpected variable loop count");

            cur = Read<byte>();
            value = (value << 7) | (cur & 0x7F);
        }

        return value;
    }

    public bool ReadBool() => Ar.Read<byte>() != 0;
}
