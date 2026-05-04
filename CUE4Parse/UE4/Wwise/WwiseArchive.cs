using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Wwise;

public sealed class FWwiseArchive(FArchive archive) : FArchive(archive.Versions)
{
    /// <summary>
    /// Wwise version, read from the BankHeader section of the .bnk file
    /// Can also be deducted from plugin version
    /// </summary>
    public uint Version;

    public FWwiseArchive(string name, byte[] data, VersionContainer? versions = null) : this(new FByteArchive(name, data, versions)) { }

    public override int Read(byte[] buffer, int offset, int count) => archive.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => archive.Seek(offset, origin);
    public override void SetLength(long value) => archive.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => archive.Write(buffer, offset, count);
    public override void Flush() => archive.Flush();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            archive.Dispose();
    }

    public override string Name => archive.Name;
    public override long Length => archive.Length;
    public override bool CanSeek => archive.CanSeek;
    public override long Position
    {
        get => archive.Position;
        set => archive.Position = value;
    }

    public override object Clone() => new FWwiseArchive(archive) { Version = Version };

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
}
