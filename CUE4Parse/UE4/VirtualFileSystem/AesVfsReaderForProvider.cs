using System;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Vfs;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public abstract partial class AbstractVfsReader
    {
        public void MountTo(FileProviderDictionary files, EventHandler<int>? vfsMounted = null)
        {
            files.AddFiles(Mount(files.IsCaseInsensitive), ReadOrder);
            vfsMounted?.Invoke(this, files.Count);
        }
    }
    public abstract partial class AbstractAesVfsReader
    {
        public void MountTo(FileProviderDictionary files, FAesKey? key, EventHandler<int>? vfsMounted = null)
        {
            AesKey = key;
            MountTo(files, vfsMounted);
        }
    }
}
