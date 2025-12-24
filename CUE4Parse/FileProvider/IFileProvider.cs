using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider
{
    public interface IFileProvider : IDisposable
    {
        /// <summary>
        /// The version container that should be used during parsing operations
        /// You can specify the game, serialization version, and custom versions here
        /// </summary>
        public VersionContainer Versions { get; }

        /// <summary>
        /// The files available in this provider in dictionary with their full path as key.
        /// </summary>
        public FileProviderDictionary Files { get; }

        public InternationalizationDictionary Internationalization { get; }

        public IDictionary<string, string> VirtualPaths { get; }

        /// <summary>
        /// the localized resources (strings) from the game
        /// </summary>
        [Obsolete("use Internationalization instead")]
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LocalizedResources => Internationalization;

        /// <summary>
        /// DefaultGame.ini file from the game
        /// </summary>
        public CustomConfigIni DefaultGame { get; }

        /// <summary>
        /// DefaultEngine.ini file from the game
        /// </summary>
        public CustomConfigIni DefaultEngine { get; }

        /// <summary>
        ///  Light units used if not specified in the asset
        ///  Located here in UE Projects DefaultEngine.ini[/Script/Engine.RendererSettings]->r.DefaultFeature.LightUnits
        /// </summary>
        public ELightUnits DefaultLightUnit { get; set; }

        /// <summary>
        /// Type Mappings that should be used for unversioned property serialization
        /// Can be null if there is no need for loading such packages
        /// </summary>
        public ITypeMappingsProvider? MappingsContainer { get; set; }

        /// <summary>
        /// Whether UStructs serialized by this file provider should read the script data
        /// </summary>
        public bool ReadScriptData { get; set; }

        /// <summary>
        /// Whether UMaterials by this file provider should read the inlined shader maps
        /// </summary>
        public bool ReadShaderMaps { get; set; }

        /// <summary>
        /// Whether file provider should read the Nanite pages
        /// </summary>
        public bool ReadNaniteData { get; set; }

        /// <summary>
        /// Whether UMaterial loading should skip loading ReferencedTextures
        /// </summary>
        public bool SkipReferencedTextures { get; set; }

        public bool UseLazyPackageSerialization { get; set; }

        /// <summary>
        /// Type Mappings for this specific game (determined by game name)
        /// </summary>
        public TypeMappings? MappingsForGame { get; }

        /// <summary>
        /// Comparison method used for file lookups<br/>
        /// Individual archive readers may use their own comparison methods if provided during mounting<br/>
        /// Has influence on <see cref="this"/> and <see cref="TryFindGameFile"/> and basically every other method that uses paths<br/>
        /// It is <see cref="StringComparer.Ordinal"/> (case-sensitive) by default
        /// </summary>
        public StringComparer PathComparer { get; }

        /// <summary>
        /// the name of the unreal project
        /// </summary>
        public string ProjectName { get; }

        /// <summary>
        /// the name of the game as displayed by its window title
        /// </summary>
        public string? GameDisplayName { get; }

        /// <summary>
        /// Searches for a game file from this provider.
        /// </summary>
        /// <param name="path">The path of the game file</param>
        /// <returns>The file found</returns>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">If there was no file with this path</exception>
        public GameFile this[string path] { get; }

        /// <summary>
        /// Attempts to find a game file from this provider.
        /// </summary>
        /// <param name="path">The path of the game file</param>
        /// <param name="file">The file if it was found; otherwise the default value</param>
        /// <returns>true if the file could be found; false otherwise</returns>
        public bool TryGetGameFile(string path, [MaybeNullWhen(false)] out GameFile file);

        public int LoadVirtualPaths();
        public int LoadVirtualPaths(FPackageFileVersion version, CancellationToken cancellationToken = default);

        public int LoadLocalization(ELanguage language = ELanguage.English, CancellationToken cancellationToken = default);
        public int LoadLocalization(string culture, CancellationToken cancellationToken = default);

        public void ChangeCulture(string culture);
        public bool TryChangeCulture(string culture);

        /// <summary>
        /// return the localized string based on params
        /// </summary>
        /// <param name="namespace">the namespace to search in</param>
        /// <param name="key">the string key</param>
        /// <param name="defaultValue">a fallback value in case the localized string doesn't exist</param>
        public string GetLocalizedString(string @namespace, string key, string defaultValue);

        /// <summary>
        /// Attempts to bring the passed path into the correct format.
        /// </summary>
        /// <param name="path">The file path to be fixed</param>
        /// <returns>The file path translated into the correct format</returns>
        public string FixPath(string path);

        #region SaveAsset Methods
        /// <summary>
        /// Loads asset data of the file with the passed path into byte[].
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The asset data</returns>
        public byte[] SaveAsset(string path);

        public byte[] SaveAsset(GameFile file);

        /// <summary>
        /// Asynchronously loads asset data of the file with the passed path into byte[].
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The asset data</returns>
        public Task<byte[]> SaveAssetAsync(string path);

        public Task<byte[]> SaveAssetAsync(GameFile file);

        /// <summary>
        /// Attempts to load asset data of the file with the passed path into byte[].
        /// </summary>
        /// <param name="path">The file path</param>
        /// <param name="data">The asset data if it was successfully loaded; otherwise default</param>
        /// <returns>true if the asset could be loaded; false otherwise</returns>
        public bool TrySaveAsset(string path, [MaybeNullWhen(false)] out byte[] data);

        public bool TrySaveAsset(GameFile file, [MaybeNullWhen(false)] out byte[] data);
        #endregion

        #region CreateReader Methods
        /// <summary>
        /// Creates a reader for the file with the passed path.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The reader</returns>
        public FArchive CreateReader(string path);

        /// <summary>
        /// Asynchronously creates a reader for the file with the passed path.
        /// Can throw various exceptions
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The reader</returns>
        public Task<FArchive> CreateReaderAsync(string path);

        /// <summary>
        /// Attempts to create a for the file with the passed path.
        /// </summary>
        /// <param name="path">The file path</param>
        /// <param name="reader">The reader if it was successfully created; otherwise default</param>
        /// <returns>true if the reader could be created; false otherwise</returns>
        public bool TryCreateReader(string path, [MaybeNullWhen(false)] out FArchive reader);
        #endregion

        #region LoadPackage Methods
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
        /// Attempts to loads and parse a Package at the passed path.
        /// </summary>
        /// <param name="path">The package file path</param>
        /// <param name="package">The parsed package content if it could be parsed; default otherwise</param>
        /// <returns>true if the package could be parsed; false otherwise</returns>
        public bool TryLoadPackage(string path, [MaybeNullWhen(false)] out IPackage package);

        /// <summary>
        /// Attempts to loads all versions of the Package.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="packages"></param>
        /// <returns></returns>
        public bool TryLoadPackages(string path, out List<IPackage> packages);

        /// <summary>
        /// Attempts to loads and parse a Package from the passed file.
        /// </summary>
        /// <param name="file">The package file</param>
        /// <param name="package">The parsed package content if it could be parsed; default otherwise</param>
        /// <returns>true if the package could be parsed; false otherwise</returns>
        public bool TryLoadPackage(GameFile file, [MaybeNullWhen(false)] out IPackage package);
        #endregion

        #region SavePackage Methods
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
        /// Attempts to load all parts of the Package at the passed path.
        /// </summary>
        /// <param name="path">The package file path</param>
        /// <param name="data">The package parts in a Dictionary with their name as keys if successfully loaded; default otherwise</param>
        /// <returns>true if the package parts could be successfully loaded; false otherwise</returns>
        public bool TrySavePackage(string path, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, byte[]> data);

        /// <summary>
        /// Attempts to load all parts of the Package in the passed file.
        /// </summary>
        /// <param name="file">The package file</param>
        /// <param name="data">The package parts in a Dictionary with their name as keys if successfully loaded; default otherwise</param>
        /// <returns>true if the package parts could be successfully loaded; false otherwise</returns>
        public bool TrySavePackage(GameFile file, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, byte[]> data);
        #endregion

        #region LoadObject Methods
        /// <summary>
        /// Loads an object from the Package at the passed path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>The loaded object</returns>
        public UObject LoadPackageObject(string path);

        /// <summary>
        /// Loads an object from the Package at the passed path with type T
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>The loaded object of type T</returns>
        public T LoadPackageObject<T>(string path) where T : UObject;

        public UObject LoadPackageObject(string path, string objectName);

        public T LoadPackageObject<T>(string path, string objectName) where T : UObject;

        /// <summary>
        /// Asynchronously loads an object from the Package at the passed path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>The loaded object</returns>
        public Task<UObject> LoadPackageObjectAsync(string path);

        /// <summary>
        /// Asynchronously loads an object from the Package at the passed path with type T
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>The loaded object of type T</returns>
        public Task<T> LoadPackageObjectAsync<T>(string path) where T : UObject;

        public Task<UObject> LoadPackageObjectAsync(string path, string objectName);

        public Task<T> LoadPackageObjectAsync<T>(string path, string objectName) where T : UObject;

        public UObject? SafeLoadPackageObject(string path);

        public T? SafeLoadPackageObject<T>(string path) where T : UObject;

        public UObject? SafeLoadPackageObject(string path, string objectName);

        public T? SafeLoadPackageObject<T>(string path, string objectName) where T : UObject;

        public Task<UObject?> SafeLoadPackageObjectAsync(string path);

        public Task<T?> SafeLoadPackageObjectAsync<T>(string path) where T : UObject;

        public Task<UObject?> SafeLoadPackageObjectAsync(string path, string objectName);

        public Task<T?> SafeLoadPackageObjectAsync<T>(string path, string objectName) where T : UObject;

        /// <summary>
        /// Attempts to load an object from the Package at the passed path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="export">The loaded object if loaded successfully; default otherwise</param>
        /// <returns>true if object was loaded; false otherwise</returns>
        public bool TryLoadPackageObject(string path, [MaybeNullWhen(false)] out UObject export);

        /// <summary>
        /// Attempts to load an object from the Package at the passed path with type T
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="export">The loaded object if loaded successfully and of correct type; default otherwise</param>
        /// <returns>true if object was loaded and of correct type; false otherwise</returns>
        public bool TryLoadPackageObject<T>(string path, [MaybeNullWhen(false)] out T export) where T : UObject;
        #endregion
    }
}
