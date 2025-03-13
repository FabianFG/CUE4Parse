using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.FileProvider.Vfs
{
    public interface IVfsFileProvider : IFileProvider
    {
        /// <summary>
        /// Global data from global io store
        /// Will only be used if the game uses io stores (.utoc and .ucas files)
        /// </summary>
        public IoGlobalData? GlobalData { get; }

        /// <summary>
        /// The files available in this provider by the FPackageId from an io store reader
        /// It only contains the id's for files from io store readers
        /// </summary>
        public IReadOnlyDictionary<FPackageId, GameFile> FilesById { get; }

        public IReadOnlyCollection<IAesVfsReader> UnloadedVfs { get; }
        public IReadOnlyCollection<IAesVfsReader> MountedVfs { get; }

        //Aes-Key Management
        public IReadOnlyDictionary<FGuid, FAesKey> Keys { get; }
        public IReadOnlyCollection<FGuid> RequiredKeys { get; }

        /// <inheritdoc cref="IAesVfsReader.CustomEncryption"/>
        public IAesVfsReader.CustomEncryptionDelegate? CustomEncryption { get; set; }
        public event EventHandler<int>? VfsRegistered;
        public event EventHandler<int>? VfsMounted;
        public event EventHandler<int>? VfsUnmounted;

        /// <summary>
        /// Scan the given directory for archives to register
        /// </summary>
        public void Initialize();

        public void RegisterVfs(string file);
        public void RegisterVfs(FileInfo file);
        public void RegisterVfs(string file, Stream[] stream, Func<string, FArchive>? openContainerStreamFunc = null);

        public int Mount();
        public Task<int> MountAsync();
        public int SubmitKey(FGuid guid, FAesKey key);
        public Task<int> SubmitKeyAsync(FGuid guid, FAesKey key);
        public int SubmitKeys(IEnumerable<KeyValuePair<FGuid, FAesKey>> keys);
        public Task<int> SubmitKeysAsync(IEnumerable<KeyValuePair<FGuid, FAesKey>> keys);

        public IAesVfsReader GetArchive(string archiveName, StringComparison comparison = StringComparison.Ordinal);
        public bool TryGetArchive(string archiveName, [MaybeNullWhen(false)] out IAesVfsReader archive, StringComparison comparison = StringComparison.Ordinal);

        public GameFile this[string path, string archiveName, StringComparison comparison = StringComparison.Ordinal] { get; }
        public GameFile this[string path, IAesVfsReader archive] { get; }

        public bool TryGetGameFile(string path, string archiveName, [MaybeNullWhen(false)] out GameFile file, StringComparison comparison = StringComparison.Ordinal);

        public byte[] SaveAsset(string path, string archiveName, StringComparison comparison = StringComparison.Ordinal);
        public byte[] SaveAsset(string path, IAesVfsReader archive);

        public FArchive CreateReader(string path, string archiveName, StringComparison comparison = StringComparison.Ordinal);
        public FArchive CreateReader(string path, IAesVfsReader archive);

        /// <summary>
        /// Loads and parses an I/O Store Package from the passed package ID.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="id">The package ID</param>
        /// <returns>The parsed package content</returns>
        public IoPackage LoadPackage(FPackageId id);

        /// <summary>
        /// Loads and parses an I/O Store Package from the passed package ID.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="id">The package ID</param>
        /// <param name="ioPackage">The parsed package content if it could be parsed; default otherwise</param>
        /// <returns>The parsed package content</returns>
        public bool TryLoadPackage(FPackageId id, [MaybeNullWhen(false)] out IoPackage ioPackage);

        /// <summary>
        /// Loads and parses a Package from the passed archive.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The package file path</param>
        /// <param name="archiveName">The archive to read from</param>
        /// <param name="comparison">The comparison to use for finding the archive</param>
        /// <returns>The parsed package content</returns>
        public IPackage LoadPackage(string path, string archiveName, StringComparison comparison = StringComparison.Ordinal);

        /// <summary>
        /// Loads and parses a Package from the passed archive.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The package file path</param>
        /// <param name="archive">The archive to read from</param>
        /// <returns>The parsed package content</returns>
        public IPackage LoadPackage(string path, IAesVfsReader archive);

        public IReadOnlyDictionary<string, byte[]> SavePackage(string path, string archiveName, StringComparison comparison = StringComparison.Ordinal);
        public IReadOnlyDictionary<string, byte[]> SavePackage(string path, IAesVfsReader archive);

        public bool TrySavePackage(string path, string archiveName, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, byte[]> data, StringComparison comparison = StringComparison.Ordinal);
    }
}
