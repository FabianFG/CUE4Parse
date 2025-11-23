using System.Linq;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects;

public struct FIoContainerMetaHeader
{
    public byte[] Magic;
    public EVersion Version;
    public uint FileCount;
    public uint DirectoryCount;
    public uint StringCount;

    private readonly byte[] _magicSequence = "CONTAINERMETAHDR"u8.ToArray();

    public FIoContainerMetaHeader(FArchive Ar)
    {
        Magic = Ar.ReadBytes(16);
        if (_magicSequence.SequenceEqual(Magic))
            throw new ParserException(Ar, "Invalid container meta header magic");

        Version = Ar.Read<EVersion>();
        FileCount = Ar.Read<uint>();
        DirectoryCount = Ar.Read<uint>();
        StringCount = Ar.Read<uint>();
        Ar.Position += 32; // Pad
    }

    public enum EVersion : uint
    {
        Invalid	= 0,
        Initial,

        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }
}
