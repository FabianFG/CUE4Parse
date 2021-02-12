using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Vfs
{
    public abstract class VfsEntry : GameFile
    {
        public readonly IVfsReader Vfs;

        public long Offset { get; protected set; }
        
        protected VfsEntry(IVfsReader vfs, string path, long size) : base(path, size, vfs.Game, vfs.Ver)
        {
            Vfs = vfs;
        }

        protected VfsEntry(IVfsReader vfs)
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