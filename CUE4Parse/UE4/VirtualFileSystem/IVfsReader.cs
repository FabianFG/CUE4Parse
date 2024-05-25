using System;
using System.Collections.Generic;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public interface IVfsReader : IDisposable
    {
        public string Path { get; }
        public string Name { get; }

        public IReadOnlyDictionary<string, GameFile> Files { get; }
        public int FileCount { get; }

        public bool HasDirectoryIndex { get; }
        public string MountPoint { get; }
        public bool IsConcurrent { get; set; }
        public bool IsMounted { get; }

        public VersionContainer Versions { get; set; }
        public EGame Game { get; set; }
        public FPackageFileVersion Ver { get; set; }

        public IReadOnlyDictionary<string, GameFile> Mount(bool caseInsensitive = false);
        public void MountTo(FileProviderDictionary files, bool caseInsensitive, EventHandler<int>? vfsMounted = null);

        public abstract byte[] Extract(VfsEntry entry);
    }
}
