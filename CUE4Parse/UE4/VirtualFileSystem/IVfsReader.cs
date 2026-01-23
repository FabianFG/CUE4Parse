using System;
using System.Collections.Generic;
using System.IO;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public interface IVfsReader : IDisposable
    {
        public string Path { get; }
        public string Name { get; }
        public long ReadOrder { get; }

        public IReadOnlyDictionary<string, GameFile> Files { get; }
        public int FileCount { get; }

        public string MountPoint { get; }
        public bool HasDirectoryIndex { get; }
        public bool IsConcurrent { get; set; }

        public VersionContainer Versions { get; set; }
        public EGame Game { get; set; }
        public FPackageFileVersion Ver { get; set; }

        public void Mount(StringComparer pathComparer);
        public void MountTo(FileProviderDictionary files, StringComparer pathComparer, EventHandler<int>? vfsMounted = null);

        public abstract byte[] Extract(VfsEntry entry, FByteBulkDataHeader? header = null);

        /// <summary>
        /// Extracts an entry as a streaming reader for on-demand decompression.
        /// Useful for large files where loading everything into memory is undesirable.
        /// </summary>
        /// <param name="entry">The entry to extract.</param>
        /// <returns>A stream for reading the entry data, or null if streaming is not supported for this entry.</returns>
        public Stream? ExtractStream(VfsEntry entry) => null;
    }
}
