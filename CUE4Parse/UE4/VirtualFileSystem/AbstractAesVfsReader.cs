using System.Runtime.CompilerServices;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public abstract partial class AbstractAesVfsReader : AbstractVfsReader, IAesVfsReader
    {
        public abstract FGuid EncryptionKeyGuid { get; }
        public abstract long Length { get; set; }
        public IAesVfsReader.CustomEncryptionDelegate? CustomEncryption { get; set; }
        public FAesKey? AesKey { get; set; }

        public abstract bool IsEncrypted { get; }
        public int EncryptedFileCount { get; protected set; }

        private static EGame _game;

        protected AbstractAesVfsReader(string path, VersionContainer versions) : base(path, versions)
        {
            _game = Game;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TestAesKey(FAesKey key) => !IsEncrypted || TestAesKey(MountPointCheckBytes(), key);

        public abstract byte[] MountPointCheckBytes();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestAesKey(byte[] bytes, FAesKey key) => IsValidIndex(_game == EGame.GAME_ApexLegendsMobile ? bytes.DecryptApexMobile(key) : bytes.Decrypt(key));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected byte[] DecryptIfEncrypted(byte[] bytes) =>
            DecryptIfEncrypted(bytes, IsEncrypted);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected byte[] DecryptIfEncrypted(byte[] bytes, int beginOffset, int count) =>
            DecryptIfEncrypted(bytes, beginOffset, count, IsEncrypted);
        protected byte[] DecryptIfEncrypted(byte[] bytes, bool isEncrypted)
        {
            if (!isEncrypted) return bytes;
            if (CustomEncryption != null)
            {
                return CustomEncryption(bytes, 0, bytes.Length, this);
            }
            if (AesKey != null && TestAesKey(AesKey))
            {
                return _game == EGame.GAME_ApexLegendsMobile ? bytes.DecryptApexMobile(AesKey) : bytes.Decrypt(AesKey);
            }
            throw new InvalidAesKeyException("Reading encrypted data requires a valid aes key");
        }
        protected byte[] DecryptIfEncrypted(byte[] bytes, int beginOffset, int count, bool isEncrypted)
        {
            if (!isEncrypted) return bytes;
            if (CustomEncryption != null)
            {
                return CustomEncryption(bytes, beginOffset, count, this);
            }
            if (AesKey != null)
            {
                return _game == EGame.GAME_ApexLegendsMobile ? bytes.DecryptApexMobile(AesKey) : bytes.Decrypt(AesKey);
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
