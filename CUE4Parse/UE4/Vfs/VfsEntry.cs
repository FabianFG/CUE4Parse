using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Vfs
{
    public abstract class VfsEntry : GameFile
    {
        public readonly IVfsReader Vfs;

        public long Offset { get; protected set; }
        
        protected VfsEntry(IVfsReader vfs, string path, long size) : base(path, size, vfs.Versions)
        {
            Vfs = vfs;
        }

        protected VfsEntry(IVfsReader vfs)
        {
            Vfs = vfs;
        }

        public override VersionContainer Versions
        {
            get => Vfs.Versions;
            protected set => Vfs.Versions = value;
        }
    }
}