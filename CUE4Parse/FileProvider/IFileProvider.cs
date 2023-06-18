using System.Collections.Generic;
using System.Threading.Tasks;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider
{
    public interface IFileProvider
    {
        /// <summary>
        /// The version container that should be used during parsing operations
        /// You can specify the game, serialization version, and custom versions here
        /// </summary>
        public VersionContainer Versions { get; set; }

        /// <summary>
        /// Type Mappings that should be used for unversioned property serialization
        /// Can be null if there is no need for loading such packages
        /// </summary>
        public ITypeMappingsProvider? MappingsContainer { get; set; }

        /// <summary>
        /// Type Mappings for this specific game (determined by game name)
        /// </summary>
        public TypeMappings? MappingsForGame { get; }

        /// <summary>
        /// the localized resources (strings) from the game
        /// </summary>
        public IDictionary<string, IDictionary<string, string>> LocalizedResources { get; }

        /// <summary>
        /// return the localized string based on params
        /// </summary>
        /// <param name="namespacee">the namespace to search in</param>
        /// <param name="key">the string key</param>
        /// <param name="defaultValue">a fallback value in case the localized string doesn't exist</param>
        public string GetLocalizedString(string namespacee, string key, string defaultValue);

        /// <summary>
        /// The files available in this provider in dictionary with their full path as key.
        /// If <see cref="IsCaseInsensitive"/> is set those keys are in lower case while the Path property of a <see cref="GameFile"/> remains in proper case
        /// </summary>
        public IReadOnlyDictionary<string, GameFile> Files { get; }

        /// <summary>
        /// The files available in this provider by the FPackageId from an io store reader
        /// It only contains the id's for files from io store readers
        /// </summary>
        public IReadOnlyDictionary<FPackageId, GameFile> FilesById { get; }

        /// <summary>
        /// Whether this file provider supports case-insensitive file lookups.
        /// Has influence to the behaviour of <see cref="Files"/> and <see cref="FixPath"/>
        /// </summary>
        public bool IsCaseInsensitive { get; }

        /// <summary>
        /// Whether UStructs serialized by this file provider should read the script data
        /// </summary>
        public bool ReadScriptData { get; set; }

        /// <summary>
        /// The name of the game represented by this provider.
        /// This is fetched from the prefix before "Game/".
        /// If there was no file with "Game/" the root folder name is returned
        /// </summary>
        public string InternalGameName { get; }

        /// <summary>
        /// Searches for a game file from this provider.
        /// </summary>
        /// <param name="path">The path of the game file</param>
        /// <returns>The found file</returns>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">If there was no file with this path</exception>
        public GameFile this[string path] { get; }

        /// <summary>
        /// Attempts to find a game file from this provider.
        /// </summary>
        /// <param name="path">The path of the game file</param>
        /// <param name="file">The file if it was found; otherwise the default value</param>
        /// <returns>true if the file could be found; false otherwise</returns>
        public bool TryFindGameFile(string path, out GameFile file);

        /// <summary>
        /// Attempts to bring the passed path into the correct format.
        /// If the <see cref="IsCaseInsensitive"/> flag is set the result will be in lowercase.
        /// </summary>
        /// <param name="path">The file path to be fixed</param>
        /// <returns>The file path translated into the correct format</returns>
        public string FixPath(string path);

        /// <summary>
        /// Loads asset data of the file with the passed path into byte[].
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The asset data</returns>
        public byte[] SaveAsset(string path);
        /// <summary>
        /// Attempts to load asset data of the file with the passed path into byte[].
        /// </summary>
        /// <param name="path">The file path</param>
        /// <param name="data">The asset data if it was successfully loaded; otherwise default</param>
        /// <returns>true if the asset could be loaded; false otherwise</returns>
        public bool TrySaveAsset(string path, out byte[] data);
        /// <summary>
        /// Asynchronously loads asset data of the file with the passed path into byte[].
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The asset data</returns>
        public Task<byte[]> SaveAssetAsync(string path);
        /// <summary>
        /// Asynchronously attempts to load asset data of the file with the passed path into byte[].
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The asset data if it was successfully loaded; null otherwise</returns>
        public Task<byte[]?> TrySaveAssetAsync(string path);

        /// <summary>
        /// Creates a reader for the file with the passed path.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The reader</returns>
        public FArchive CreateReader(string path);
        /// <summary>
        /// Attempts to create a for the file with the passed path.
        /// </summary>
        /// <param name="path">The file path</param>
        /// <param name="reader">The reader if it was successfully created; otherwise default</param>
        /// <returns>true if the reader could be created; false otherwise</returns>
        public bool TryCreateReader(string path, out FArchive reader);
        /// <summary>
        /// Asynchronously creates a reader for the file with the passed path.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The reader</returns>
        public Task<FArchive> CreateReaderAsync(string path);
        /// <summary>
        /// Asynchronously attempts to create a reader for the file with the passed path.
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The reader if it could be created; null otherwise</returns>
        public Task<FArchive?> TryCreateReaderAsync(string path);
        /// <summary>
        /// Loads and parses a Package at the passed path.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The package file path</param>
        /// <returns>The parsed package content</returns>
        public IPackage LoadPackage(string path);
        /// <summary>
        /// Loads and parses a Package from the passed file.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="file">The package file</param>
        /// <returns>The parsed package content</returns>
        public IPackage LoadPackage(GameFile file);
        /// <summary>
        /// Loads and parses an I/O Store Package from the passed package ID.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="id">The package ID</param>
        /// <returns>The parsed package content</returns>
        public IoPackage LoadPackage(FPackageId id);
        /// <summary>
        /// Attempts to loads and parse a Package at the passed path.
        /// </summary>
        /// <param name="path">The package file path</param>
        /// <param name="package">The parsed package content if it could be parsed; default otherwise</param>
        /// <returns>true if the package could be parsed; false otherwise</returns>
        public bool TryLoadPackage(string path, out IPackage package);
        /// <summary>
        /// Attempts to loads and parse a Package from the passed file.
        /// </summary>
        /// <param name="file">The package file</param>
        /// <param name="package">The parsed package content if it could be parsed; default otherwise</param>
        /// <returns>true if the package could be parsed; false otherwise</returns>
        public bool TryLoadPackage(GameFile file, out IPackage package);
        /// <summary>
        /// Attempts to load and parse an I/O Store Package from the passed package ID.
        /// </summary>
        /// <param name="id">The package ID</param>
        /// <param name="package">The parsed package content if it could be parsed; default otherwise</param>
        /// <returns>true if the package could be parsed; false otherwise</returns>
        public bool TryLoadPackage(FPackageId id, out IoPackage package);
        /// <summary>
        /// Asynchronously loads and parses a Package at the passed path.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The package file path</param>
        /// <returns>The parsed package content</returns>
        public Task<IPackage> LoadPackageAsync(string path);
        /// <summary>
        /// Asynchronously loads and parses a Package from the passed file.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="file">The package file</param>
        /// <returns>The parsed package content</returns>
        public Task<IPackage> LoadPackageAsync(GameFile file);
        /// <summary>
        /// Asynchronously attempts to loads and parse a Package at the passed path.
        /// </summary>
        /// <param name="path">The package file path</param>
        /// <returns>The parsed package content if it could be parsed; default otherwise</returns>
        public Task<IPackage?> TryLoadPackageAsync(string path);
        /// <summary>
        /// Asynchronously attempts to loads and parse a Package for the passed file.
        /// </summary>
        /// <param name="file">The package file</param>
        /// <returns>The parsed package content if it could be parsed; default otherwise</returns>
        public Task<IPackage?> TryLoadPackageAsync(GameFile file);
        /// <summary>
        /// Loads all parts of the Package at the passed path.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The package file path</param>
        /// <returns>The package parts in a Dictionary with their name as keys</returns>
        public IReadOnlyDictionary<string, byte[]> SavePackage(string path);
        /// <summary>
        /// Loads all parts of the Package in the passed file.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="file">The package file</param>
        /// <returns>The package parts in a Dictionary with their name as keys</returns>
        public IReadOnlyDictionary<string, byte[]> SavePackage(GameFile file);
        /// <summary>
        /// Attempts to load all parts of the Package at the passed path.
        /// </summary>
        /// <param name="path">The package file path</param>
        /// <param name="package">The package parts in a Dictionary with their name as keys if successfully loaded; default otherwise</param>
        /// <returns>true if the package parts could be successfully loaded; false otherwise</returns>
        public bool TrySavePackage(string path, out IReadOnlyDictionary<string, byte[]> package);
        /// <summary>
        /// Attempts to load all parts of the Package in the passed file.
        /// </summary>
        /// <param name="file">The package file</param>
        /// <param name="package">The package parts in a Dictionary with their name as keys if successfully loaded; default otherwise</param>
        /// <returns>true if the package parts could be successfully loaded; false otherwise</returns>
        public bool TrySavePackage(GameFile file, out IReadOnlyDictionary<string, byte[]> package);
        /// <summary>
        /// Asynchronously loads all parts of the Package at the passed path.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The package file path</param>
        /// <returns>The package parts in a Dictionary with their name as keys</returns>
        public Task<IReadOnlyDictionary<string, byte[]>> SavePackageAsync(string path);
        /// <summary>
        /// Asynchronously loads all parts of the Package in the passed file.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="file">The package file</param>
        /// <returns>The package parts in a Dictionary with their name as keys</returns>
        public Task<IReadOnlyDictionary<string, byte[]>> SavePackageAsync(GameFile file);
        /// <summary>
        /// Asynchronously attempts to load all parts of the Package at the passed path.
        /// </summary>
        /// <param name="path">The package file path</param>
        /// <returns>The package parts in a Dictionary with their name as keys if successfully loaded; null otherwise</returns>
        public Task<IReadOnlyDictionary<string, byte[]>?> TrySavePackageAsync(string path);
        /// <summary>
        /// Asynchronously attempts to load all parts of the Package in the passed file.
        /// </summary>
        /// <param name="file">The package file</param>
        /// <returns>The package parts in a Dictionary with their name as keys if successfully loaded; null otherwise</returns>
        public Task<IReadOnlyDictionary<string, byte[]>?> TrySavePackageAsync(GameFile file);

        /// <summary>
        /// Loads an object from the Package at the passed path
        /// </summary>
        /// <param name="objectPath">The object path</param>
        /// <returns>The loaded object</returns>
        public UObject LoadObject(string? objectPath);
        /// <summary>
        /// Attempts to load an object from the Package at the passed path
        /// </summary>
        /// <param name="objectPath">The object path</param>
        /// <param name="export">The loaded object if loaded successfully; default otherwise</param>
        /// <returns>true if object was loaded; false otherwise</returns>
        public bool TryLoadObject(string? objectPath, out UObject export);
        /// <summary>
        /// Loads an object from the Package at the passed path with type T
        /// </summary>
        /// <param name="objectPath">The object path</param>
        /// <returns>The loaded object of type T</returns>
        public T LoadObject<T>(string? objectPath) where T : UObject;
        /// <summary>
        /// Attempts to load an object from the Package at the passed path with type T
        /// </summary>
        /// <param name="objectPath">The object path</param>
        /// <param name="export">The loaded object if loaded successfully and of correct type; default otherwise</param>
        /// <returns>true if object was loaded and of correct type; false otherwise</returns>
        public bool TryLoadObject<T>(string? objectPath, out T export) where T : UObject;
        /// <summary>
        /// Asynchronously loads an object from the Package at the passed path
        /// </summary>
        /// <param name="objectPath">The object path</param>
        /// <returns>The loaded object</returns>
        public Task<UObject> LoadObjectAsync(string? objectPath);
        /// <summary>
        /// Asynchronously attempts to load an object from the Package at the passed path
        /// </summary>
        /// <param name="objectPath">The object path</param>
        /// <returns>The loaded object if loaded successfully; null otherwise</returns>
        public Task<UObject?> TryLoadObjectAsync(string? objectPath);
        /// <summary>
        /// Asynchronously loads an object from the Package at the passed path with type T
        /// </summary>
        /// <param name="objectPath">The object path</param>
        /// <returns>The loaded object of type T</returns>
        public Task<T> LoadObjectAsync<T>(string? objectPath) where T : UObject;
        /// <summary>
        /// Asynchronously attempts to load an object from the Package at the passed path with type T
        /// </summary>
        /// <param name="objectPath">The object path</param>
        /// <returns>The loaded object if loaded successfully and of correct type; null otherwise</returns>
        public Task<T?> TryLoadObjectAsync<T>(string? objectPath) where T : UObject;

        /// <summary>
        /// Loads all objects from the Package at the passed path
        /// </summary>
        /// <param name="packagePath">The package path</param>
        /// <returns>All objects of the package</returns>
        public IEnumerable<UObject> LoadAllObjects(string? packagePath);
    }
}
