using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.Compression;

public class FCompressedBuffer
{
    public FCompressedBufferHeader Header;
    public byte[] Data;

    public FCompressedBuffer(FArchive Ar)
    {
        Header = new FCompressedBufferHeader(Ar);

        const ulong MaxCompressedSize = (ulong)1 << 48;
        ulong headerSize = 64; // hardcode for now
        if (Header.Magic == FCompressedBufferHeader.ExpectedMagic &&
            Header.TotalCompressedSize >= headerSize &&
            Header.TotalCompressedSize <= MaxCompressedSize)
        {
            Data = Ar.ReadArray<byte>((int)(Header.TotalCompressedSize - headerSize));
        }
        else
        {
            Data = [];
        }
    }
}
