using System.Runtime.CompilerServices;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.VirtualFileSystem;

public abstract partial class AbstractAesVfsReader : AbstractVfsReader, IAesVfsReader
{
    public abstract long Length { get; set; }
    public IAesVfsReader.CustomEncryptionDelegate? CustomEncryption { get; set; }
    public FAesKey? AesKey { get; set; }

    public abstract FGuid EncryptionKeyGuid { get; }
    public abstract bool IsEncrypted { get; }

    public int EncryptedFileCount { get; protected set; }
    public bool bDecrypted { get; protected set; }

    private static EGame _game;

    protected AbstractAesVfsReader(string path, VersionContainer versions) : base(path, versions)
    {
        _game = Game;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TestAesKey(FAesKey key) => !IsEncrypted || TestAesKey(MountPointCheckBytes(), key);

    public abstract byte[] MountPointCheckBytes();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TestAesKey(byte[] bytes, FAesKey key)
    {
        return IsValidIndex(bytes.Decrypt(key));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected byte[] Decrypt(byte[] bytes, FAesKey? key, bool bypassMountPointCheck = false)
    {
        if (bDecrypted)
        {
            return bytes.Decrypt(key!);
        }

        if (key != null && (TestAesKey(key) || bypassMountPointCheck))
        {
            bDecrypted = true;
            return bytes.Decrypt(key!);
        }
        throw new InvalidAesKeyException("Reading encrypted data requires a valid aes key");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected byte[] DecryptIfEncrypted(byte[] bytes) => DecryptIfEncrypted(bytes, IsEncrypted);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected byte[] DecryptIfEncrypted(byte[] bytes, int beginOffset, int count) =>
        DecryptIfEncrypted(bytes, beginOffset, count, IsEncrypted);

    protected byte[] DecryptIfEncrypted(byte[] bytes, bool isEncrypted)
    {
        if (!isEncrypted) return bytes;
        if (CustomEncryption != null)
        {
            return CustomEncryption(bytes, 0, bytes.Length, true, this);
        }

        return Decrypt(bytes, AesKey);
    }

    protected byte[] DecryptIfEncrypted(byte[] bytes, int beginOffset, int count, bool isEncrypted, bool bypassMountPointCheck = false)
    {
        if (!isEncrypted) return bytes;
        if (CustomEncryption != null)
        {
            return CustomEncryption(bytes, beginOffset, count, false, this);
        }

        return Decrypt(bytes, AesKey, bypassMountPointCheck);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract byte[] ReadAndDecrypt(int length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected byte[] ReadAndDecrypt(int length, FArchive reader, bool isEncrypted) =>
        DecryptIfEncrypted(reader.ReadBytes(length), isEncrypted);

    protected byte[] ReadAndDecryptAt(long position, int length, FArchive reader, bool isEncrypted) =>
        DecryptIfEncrypted(reader.ReadBytesAt(position, length), isEncrypted);
}
