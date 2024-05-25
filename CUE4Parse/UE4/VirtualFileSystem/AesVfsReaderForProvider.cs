using System;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Vfs;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public abstract partial class AbstractVfsReader
    {
        public void MountTo(FileProviderDictionary files, bool caseInsensitive, EventHandler<int>? vfsMounted = null)
        {
            files.AddFiles(Mount(caseInsensitive));
            vfsMounted?.Invoke(this, files.Count);
        }
    }
    public abstract partial class AbstractAesVfsReader
    {
        public void MountTo(FileProviderDictionary files, bool caseInsensitive, FAesKey? key, EventHandler<int>? vfsMounted = null)
        {
            AesKey = key;
            MountTo(files, caseInsensitive, vfsMounted);
        }
    }
}
