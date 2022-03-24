using CUE4Parse.FileProvider;

namespace CUE4Parse.UE4.Vfs
{
    public abstract class VfsEntry : GameFile
    {
        public readonly IVfsReader Vfs;

        public long Offset { get; protected set; }

        public override int ReadOrder => Vfs.ReadOrder;
        
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