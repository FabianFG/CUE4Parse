using System.Runtime.CompilerServices;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Vfs
{
    public abstract partial class AbstractAesVfsReader : AbstractVfsReader, IAesVfsReader
    {
        public abstract FGuid EncryptionKeyGuid { get; }
        public FAesKey? AesKey { get; set; }
        
        public abstract bool IsEncrypted { get; }
        public int EncryptedFileCount { get; protected set; }
        
        protected AbstractAesVfsReader(string path, EGame game, UE4Version ver) : base(path, game, ver)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TestAesKey(FAesKey key) => !IsEncrypted ? true : TestAesKey(MountPointCheckBytes(), key);

        public abstract byte[] MountPointCheckBytes();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestAesKey(byte[] bytes, FAesKey key) => IsValidIndex(bytes.Decrypt(key));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected byte[] DecryptIfEncrypted(byte[] bytes) =>
            DecryptIfEncrypted(bytes, IsEncrypted);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected byte[] DecryptIfEncrypted(byte[] bytes, int beginOffset, int count) =>
            DecryptIfEncrypted(bytes, beginOffset, count, IsEncrypted);
        protected byte[] DecryptIfEncrypted(byte[] bytes, bool isEncrypted)
        {
            if (!isEncrypted) return bytes;
            if (AesKey != null)
            {
                return bytes.Decrypt(AesKey);
            }
            throw new InvalidAesKeyException("Reading encrypted data requires a valid aes key");
        }
        protected byte[] DecryptIfEncrypted(byte[] bytes, int beginOffset, int count, bool isEncrypted)
        {
            if (!isEncrypted) return bytes;
            if (AesKey != null)
            {
                return bytes.Decrypt(beginOffset, count, AesKey);
            }
            throw new InvalidAesKeyException("Reading encrypted data requires a valid aes key");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract byte[] ReadAndDecrypt(int length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected byte[] ReadAndDecrypt(int length, FArchive reader, bool isEncrypted) =>
            DecryptIfEncrypted(reader.ReadBytes(length), isEncrypted);
    }
}