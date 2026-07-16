using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.IO;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public abstract partial class AbstractVfsReader
    {
        public void MountTo(FileProviderDictionary files, StringComparer pathComparer, EventHandler<int>? vfsMounted = null)
        {
            Mount(pathComparer);

            files.AddFiles(Files, ReadOrder, this is IoStoreReader ioStoreReader ? ioStoreReader.PackageIdIndex : null);
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
