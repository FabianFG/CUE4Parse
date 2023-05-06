using CUE4Parse.FileProvider.Objects;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public abstract class VfsEntry : GameFile
    {
        public readonly IVfsReader Vfs;

        public long Offset { get; protected set; }

        protected VfsEntry(IVfsReader vfs, string path, long size) : base(path, size)
        {
            Vfs = vfs;
        }

        protected VfsEntry(IVfsReader vfs)
        {
            Vfs = vfs;
        }
    }
}
