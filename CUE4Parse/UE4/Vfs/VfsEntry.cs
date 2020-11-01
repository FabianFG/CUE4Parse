using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Vfs
{
    public abstract class VfsEntry : GameFile
    {
        public readonly VirtualFileSystemReader Vfs;

        protected VfsEntry(VirtualFileSystemReader vfs, string path, long size) : base(path, size, vfs.Ver, vfs.Game)
        {
            Vfs = vfs;
        }

        public override UE4Version Ver
        {
            get => Vfs.Ver;
            protected set => Vfs.Ver = value;
        }
        public override EGame Game
        {
            get => Vfs.Game;
            protected set => Vfs.Game = value;
        }
    }
}