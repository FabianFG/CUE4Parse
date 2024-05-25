using System;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public interface IAesVfsReader : IVfsReader
    {
        public FGuid EncryptionKeyGuid { get; }
        public long Length { get; set; }

        public CustomEncryptionDelegate? CustomEncryption { get; set; }
        public FAesKey? AesKey { get; set; }

        public bool IsEncrypted { get; }
        public int EncryptedFileCount { get; }
        public bool TestAesKey(FAesKey key);
        public byte[] MountPointCheckBytes();

        public void MountTo(FileProviderDictionary files, bool caseInsensitive, FAesKey? key, EventHandler<int>? vfsMounted = null);

        public delegate byte[] CustomEncryptionDelegate(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader);
    }
}
