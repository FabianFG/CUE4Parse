using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Vfs
{
    public interface IAesVfsReader : IVfsReader
    {
        public FGuid EncryptionKeyGuid { get; }
        public FAesKey? AesKey { get; set; }
        
        public bool IsEncrypted { get; }
        public int EncryptedFileCount { get; }
        public bool TestAesKey(FAesKey key);
        public byte[] MountPointCheckBytes();

        public void MountTo(FileProviderDictionary files, bool caseInsensitive, FAesKey? key);
    }
}