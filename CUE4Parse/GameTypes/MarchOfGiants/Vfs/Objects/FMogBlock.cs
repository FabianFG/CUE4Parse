using CUE4Parse.Encryption.Aes;
using CUE4Parse.GameTypes.MarchOfGiants.Encryption;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.GameTypes.MarchOfGiants.Vfs.Objects;

internal readonly record struct FMogChunk(FMogBlock? Block, int Offset, int Size);
internal sealed record FMogCachedBlock(FMogBlock Block, Lazy<byte[]> Data);
internal sealed class FMogBlock
{
    private FAesKey? _key;
    public FGuid Hash { get; }
    public long Offset { get; }
    public int CompressedSize { get; }
    public int Size { get; }
    public int EncryptedSize => (int) ((long) CompressedSize).Align(Aes.ALIGN);
    public bool IsCompressed => CompressedSize != Size;
    public FAesKey Key => _key ??= MogEncryption.GetKey(Hash.AsByteSpan());

    public FMogBlock(FByteArchive Ar, long offset)
    {
        Hash = new FGuid(Ar.ReadSpan(24)[..16]);
        Offset = offset;
        CompressedSize = Ar.Read<int>();
        Size = Ar.Read<int>();
    }
}
