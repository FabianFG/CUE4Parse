using CUE4Parse.FileProvider.Objects;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public abstract class VfsEntry : GameFile
    {
        public IVfsReader Vfs { get; }
        public long Offset { get; protected init; }

        protected VfsEntry(IVfsReader vfs, string path, long size = 0) : base(path, size)
        {
            Vfs = vfs;
        }

        protected VfsEntry(IVfsReader vfs)
        {
            Vfs = vfs;
        }
    }
}
