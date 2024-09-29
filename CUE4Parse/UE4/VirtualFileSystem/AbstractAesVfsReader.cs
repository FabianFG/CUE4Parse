using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Aes = System.Security.Cryptography.Aes;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public abstract partial class AbstractAesVfsReader : AbstractVfsReader, IAesVfsReader
    {
        public abstract long Length { get; set; }
        public IAesVfsReader.CustomEncryptionDelegate? CustomEncryption { get; set; }
        public FAesKey? AesKey { get; set; }

        public abstract FGuid EncryptionKeyGuid { get; }
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
        public static bool TestAesKey(byte[] bytes, FAesKey key)
        {
            var decrypted = _game switch
            {
                EGame.GAME_ApexLegendsMobile => bytes.DecryptApexMobile(key),
                _ => bytes.Decrypt(key)
            };

            return IsValidIndex(decrypted);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected byte[] Decrypt(byte[] bytes, FAesKey? key, bool bypassMountPointCheck = false)
        {
            if (key != null)
            {
                var valid = TestAesKey(key) || bypassMountPointCheck;
                if (!valid && _game == EGame.GAME_Snowbreak)
                {
                    var newKey = ConvertSnowbreakAes(Name, key);
                    if (TestAesKey(newKey))
                    {
                        valid = true;
                        key = newKey;
                    }
                }

                if (valid)
                {
                    return _game switch
                    {
                        EGame.GAME_ApexLegendsMobile => bytes.DecryptApexMobile(key),
                        _ => bytes.Decrypt(key)
                    };
                }
            }
            throw new InvalidAesKeyException("Reading encrypted data requires a valid aes key");
        }

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

        private static FAesKey ConvertSnowbreakAes(string name, FAesKey key)
        {
            var pakName = System.IO.Path.GetFileNameWithoutExtension(name).ToLower();
            var pakNameBytes = Encoding.ASCII.GetBytes(pakName);
            var md5Name = MD5.HashData(pakNameBytes);

            var md5AsString = Convert.ToHexString(md5Name).ToLower();
            var md5StrBytes = Encoding.ASCII.GetBytes(md5AsString);

            using var aesEnc = Aes.Create();
            aesEnc.Mode = CipherMode.ECB;
            aesEnc.Key = key.Key;

            var newKey = new byte[32];
            aesEnc.EncryptEcb(md5StrBytes, newKey, PaddingMode.None);

            return new FAesKey(newKey);
        }
    }
}
