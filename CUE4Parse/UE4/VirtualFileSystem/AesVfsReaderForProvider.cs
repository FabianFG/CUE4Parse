using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Vfs;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public abstract partial class AbstractVfsReader
    {
        public void MountTo(FileProviderDictionary files, bool caseInsensitive)
        {
            files.AddFiles(Mount(caseInsensitive));
        }
    }
    public abstract partial class AbstractAesVfsReader
    {
        public void MountTo(FileProviderDictionary files, bool caseInsensitive, FAesKey? key)
        {
            AesKey = key;
            MountTo(files, caseInsensitive);
        }
    }
}
