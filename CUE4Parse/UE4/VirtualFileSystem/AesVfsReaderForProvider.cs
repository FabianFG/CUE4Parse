using System;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Vfs;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public abstract partial class AbstractVfsReader
    {
        public void MountTo(FileProviderDictionary files, StringComparer pathComparer, EventHandler<int>? vfsMounted = null)
        {
            Mount(pathComparer);

            files.AddFiles(Files, ReadOrder);
            vfsMounted?.Invoke(this, files.Count);
        }
    }
    public abstract partial class AbstractAesVfsReader
    {
        public void MountTo(FileProviderDictionary files, StringComparer pathComparer, FAesKey? key, EventHandler<int>? vfsMounted = null)
        {
            AesKey = key;
            MountTo(files, pathComparer, vfsMounted);
        }
    }
}
