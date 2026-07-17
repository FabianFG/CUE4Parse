using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
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
    /// <summary>
    /// Custom encryption delegate for ciphers that derive their state from container offsets.
    /// The absolute offset identifies the first requested byte, while the encryption base identifies
    /// the start of its continuous cipher stream.
    /// </summary>
    public IAesVfsReader.CustomEncryptionWithOffsetDelegate? CustomEncryptionWithOffset { get; set; }
    public FAesKey? AesKey { get; set; }
    public CompressionMethod[] CompressionMethods { get; set; }

    public abstract FGuid EncryptionKeyGuid { get; }
    public abstract bool IsEncrypted { get; }

    public int EncryptedFileCount { get; protected set; }
    public bool bDecrypted { get; protected set; }

    protected AbstractAesVfsReader(string path, VersionContainer versions) : base(path, versions)
    {
        // yes
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TestAesKey(FAesKey key) => !IsEncrypted || TestAesKey(MountPointCheckBytes(), key);

    public abstract byte[] MountPointCheckBytes();
    protected virtual long MountPointCheckOffset => 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TestAesKey(byte[] bytes, FAesKey key)
    {
        byte[] result;
        if (CustomEncryptionWithOffset != null || CustomEncryption != null)
        {
            var backupKey = AesKey;
            AesKey = key;
            try
            {
                result = CustomEncryptionWithOffset != null
                    ? CustomEncryptionWithOffset(bytes, 0, bytes.Length, true, MountPointCheckOffset, MountPointCheckOffset, this)
                    : CustomEncryption!(bytes, 0, bytes.Length, true, this);
            }
            finally { AesKey = backupKey; }
        }
        else
        {
            result = bytes.Decrypt(key);
        }

        return IsValidIndex(result);
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

    protected byte[] DecryptIfEncrypted(byte[] bytes, bool isEncrypted, bool isIndex = false) =>
        DecryptIfEncryptedAtOffset(bytes, isEncrypted, isIndex, 0, 0);

    private byte[] DecryptIfEncryptedAtOffset(byte[] bytes, bool isEncrypted, bool isIndex, long absoluteOffset,
        long encryptionBaseOffset)
    {
        if (!isEncrypted) return bytes;
        if (CustomEncryptionWithOffset != null)
        {
            return CustomEncryptionWithOffset(bytes, 0, bytes.Length, isIndex, absoluteOffset,
                encryptionBaseOffset, this);
        }
        if (CustomEncryption != null)
        {
            return CustomEncryption(bytes, 0, bytes.Length, isIndex, this);
        }

        return Decrypt(bytes, AesKey);
    }

    protected byte[] DecryptIfEncrypted(byte[] bytes, int beginOffset, int count, bool isEncrypted,
        bool bypassMountPointCheck = false, bool isIndex = false) =>
        DecryptIfEncryptedAtOffset(bytes, beginOffset, count, isEncrypted, bypassMountPointCheck, isIndex, 0, 0);

    private byte[] DecryptIfEncryptedAtOffset(byte[] bytes, int beginOffset, int count, bool isEncrypted,
        bool bypassMountPointCheck, bool isIndex, long absoluteOffset, long encryptionBaseOffset)
    {
        if (!isEncrypted) return bytes;
        if (CustomEncryptionWithOffset != null)
        {
            return CustomEncryptionWithOffset(bytes, beginOffset, count, isIndex, absoluteOffset,
                encryptionBaseOffset, this);
        }
        if (CustomEncryption != null)
        {
            return CustomEncryption(bytes, beginOffset, count, isIndex, this);
        }

        return Decrypt(bytes, AesKey, bypassMountPointCheck);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract byte[] ReadAndDecrypt(int length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual byte[] ReadAndDecryptIndex(int length) => ReadAndDecrypt(length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected byte[] ReadAndDecrypt(int length, FArchive reader, bool isEncrypted) =>
        ReadAndDecryptAt(reader.Position, length, reader, isEncrypted);

    protected byte[] ReadAndDecryptAt(long position, int length, FArchive reader, bool isEncrypted) =>
        DecryptIfEncryptedAtOffset(reader.ReadBytesAt(position, length), isEncrypted, false, position, position);

    protected byte[] ReadAndDecryptAtWithBase(long position, int length, FArchive reader, bool isEncrypted,
        long encryptionBaseOffset) =>
        DecryptIfEncryptedAtOffset(reader.ReadBytesAt(position, length), isEncrypted, false, position,
            encryptionBaseOffset);

    protected byte[] ReadAndDecryptAt(byte[] buffer, long position, int length, FArchive reader, bool isEncrypted)
    {
        reader.ReadAt(position, buffer, 0, length);
        return DecryptIfEncrypted(buffer, isEncrypted);
    }

    protected byte[] ReadAndDecryptAtWithBase(byte[] buffer, long position, int length, FArchive reader,
        bool isEncrypted, long encryptionBaseOffset)
    {
        reader.ReadAt(position, buffer, 0, length);
        if (isEncrypted && CustomEncryptionWithOffset != null)
        {
            return DecryptIfEncryptedAtOffset(buffer, 0, length, true, false, false, position,
                encryptionBaseOffset);
        }
        return DecryptIfEncryptedAtOffset(buffer, isEncrypted, false, position, encryptionBaseOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected byte[] ReadAndDecryptIndex(int length, FArchive reader, bool isEncrypted)
    {
        var position = reader.Position;
        return DecryptIfEncryptedAtOffset(reader.ReadBytes(length), isEncrypted, true, position, position);
    }

    protected byte[] ReadAndDecryptIndexAt(long position, int length, FArchive reader, bool isEncrypted) =>
        DecryptIfEncryptedAtOffset(reader.ReadBytesAt(position, length), isEncrypted, true, position, position);
}
