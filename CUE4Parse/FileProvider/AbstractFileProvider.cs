using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Plugins;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Serilog;
using UE4Config.Parsing;

namespace CUE4Parse.FileProvider
{
    public class CustomConfigIni : ConfigIni
    {
        /// <summary>
        /// guid of the archive that owns this config
        /// </summary>
        public FGuid? EncryptionKeyGuid { get; set; }

        public CustomConfigIni(string name) : base(name) { }
    }

    public abstract class AbstractFileProvider : IFileProvider
    {
        protected static readonly ILogger Log = Serilog.Log.ForContext<IFileProvider>();

        public VersionContainer Versions { get; }
        public StringComparer PathComparer { get; }

        public FileProviderDictionary Files { get; }
        public InternationalizationDictionary Internationalization { get; }
        public IDictionary<string, string> VirtualPaths { get; }
        public CustomConfigIni DefaultGame { get; }
        public CustomConfigIni DefaultEngine { get; }

        public ITypeMappingsProvider? MappingsContainer { get; set; }
        public bool ReadScriptData { get; set; }
        public bool ReadShaderMaps { get; set; }
        public bool SkipReferencedTextures { get; set; }
        public bool UseLazyPackageSerialization { get; set; } = true;

        public TypeMappings? MappingsForGame => MappingsContainer?.MappingsForGame;

        protected AbstractFileProvider(VersionContainer? versions = null, StringComparer? pathComparer = null)
        {
            Versions = versions ?? VersionContainer.DEFAULT_VERSION_CONTAINER;
            PathComparer = pathComparer ?? StringComparer.Ordinal;

            Files = new FileProviderDictionary();
            Internationalization = new InternationalizationDictionary(PathComparer);
            VirtualPaths = new Dictionary<string, string>(PathComparer);
            DefaultGame = new CustomConfigIni(nameof(DefaultGame));
            DefaultEngine = new CustomConfigIni(nameof(DefaultEngine));
        }

        private string? _gameDisplayName;
        public string? GameDisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(_gameDisplayName))
                {
                    var inst = new List<InstructionToken>();
                    DefaultGame.FindPropertyInstructions("/Script/EngineSettings.GeneralProjectSettings", "ProjectDisplayedTitle", inst);
                    if (inst.Count > 0)
                    {
                        var projectMatch = Regex.Match(inst[0].Value, "^(?:NSLOCTEXT\\(\".*\", \".*\", \"(?'target'.*)\"\\)|(?:INVTEXT\\(\"(?'target'.*)\"\\))|(?'target'.*))$", RegexOptions.Singleline);
                        if (projectMatch.Groups.TryGetValue("target", out var g))
                        {
                            if (g.Value.StartsWith("LOCTABLE(\"/Game/"))
                            {
                                var stringTablePath = g.Value.SubstringAfter("LOCTABLE(\"").SubstringBeforeLast("\",");

                                if (TryLoadPackageObject<UStringTable>(stringTablePath, out var stringTable))
                                {
                                    var keyName = g.Value.SubstringAfterLast(", \"").SubstringBeforeLast("\")"); // LOCTABLE("/Game/Narrative/LocalisedStrings/UI_Strings.UI_Strings", "23138_ui_pc_game_name_titlebar")
                                    var stringTableEntry = stringTable.StringTable.KeysToEntries;
                                    if (stringTableEntry.TryGetValue(keyName, out var value))
                                    {
                                        _gameDisplayName = value;
                                    }
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(g.Value) && g.Value != "{GameName}")
                            {
                                _gameDisplayName = g.Value;
                            }
                            else
                            {
                                inst.Clear();
                                DefaultGame.FindPropertyInstructions("/Script/EngineSettings.GeneralProjectSettings", "ProjectName", inst);
                                if (inst.Count > 0) _gameDisplayName = inst[0].Value;
                            }
                        }
                    }
                    else
                    {
                        DefaultGame.FindPropertyInstructions("/Script/EngineSettings.GeneralProjectSettings", "ProjectName", inst);
                        if (inst.Count > 0) _gameDisplayName = inst[0].Value;
                    }
                }
                return _gameDisplayName;
            }
        }

        private string? _projectName;
        public string ProjectName
        {
            get
            {
                if (string.IsNullOrEmpty(_projectName))
                {
                    if (Files.Keys.FirstOrDefault(it => it.EndsWith(".uproject", StringComparison.OrdinalIgnoreCase)) is not { } t)
                    {
                        t = Files.Keys.FirstOrDefault(
                            it => !it.StartsWith('/') && it.Contains('/') &&
                                  !it.SubstringBefore('/').EndsWith("Engine", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
                    }

                    _projectName = t.SubstringBefore('/');
                    if (PathComparer.Equals(_projectName, "MidnightSuns"))
                        _projectName = "CodaGame";
                }
                return _projectName;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryGetGameFile(string path, IReadOnlyDictionary<string, GameFile> collection, [MaybeNullWhen(false)] out GameFile file)
        {
            var fixedPath = FixPath(path);
            if (!collection.TryGetValue(fixedPath, out file) && // any extension
                !collection.TryGetValue(fixedPath.SubstringBeforeWithLast('.') + GameFile.UePackageExtensions[1], out file) && // umap
                !collection.TryGetValue(path, out file)) // in case FixPath broke something
            {
                file = null;
            }

            return file != null;
        }

        public GameFile this[string path]
            => TryGetGameFile(path, Files, out var file)
                ? file
                : throw new KeyNotFoundException($"There is no game file with the path \"{path}\"");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetGameFile(string path, [MaybeNullWhen(false)] out GameFile file)
        {
            try
            {
                file = this[path];
            }
            catch
            {
                file = null;
            }
            return file != null;
        }

        public int LoadLocalization(ELanguage language = ELanguage.English, CancellationToken cancellationToken = default)
            => LoadLocalization(GetLanguageCode(language), cancellationToken);

        [Obsolete("use Provider.ChangeCulture instead")]
        public int LoadLocalization(string culture, CancellationToken cancellationToken = default)
        {
            Internationalization.ChangeCulture(culture, Files);
            return Internationalization.Count;
        }

        public void ChangeCulture(string culture) => Internationalization.ChangeCulture(culture, Files);

        public bool TryChangeCulture(string culture)
        {
            try
            {
                ChangeCulture(culture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [Obsolete("use Internationalization.SafeGet instead")]
        public string GetLocalizedString(string @namespace, string key, string? defaultValue)
            => Internationalization.SafeGet(@namespace, key, defaultValue);

        /// <summary>
        /// TODO: get rid of this
        /// either read the culture from .locmeta files or from inis
        /// </summary>
        public string GetLanguageCode(ELanguage language)
        {
            return ProjectName.ToLowerInvariant() switch
            {
                "fortnitegame" => language switch
                {
                    ELanguage.English => "en",
                    ELanguage.French => "fr",
                    ELanguage.German => "de",
                    ELanguage.Italian => "it",
                    ELanguage.Spanish => "es",
                    ELanguage.SpanishLatin => "es-419",
                    ELanguage.Arabic => "ar",
                    ELanguage.Japanese => "ja",
                    ELanguage.Korean => "ko",
                    ELanguage.Polish => "pl",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru",
                    ELanguage.Turkish => "tr",
                    ELanguage.Chinese => "zh-CN",
                    ELanguage.TraditionalChinese => "zh-Hant",
                    _ => "en"
                },
                "worldexplorers" => language switch
                {
                    ELanguage.English => "en",
                    ELanguage.French => "fr",
                    ELanguage.German => "de",
                    ELanguage.Italian => "it",
                    ELanguage.Spanish => "es",
                    ELanguage.Japanese => "ja",
                    ELanguage.Korean => "ko",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru",
                    ELanguage.Chinese => "zh-Hans",
                    _ => "en"
                },
                "shootergame" => language switch
                {
                    ELanguage.English => "en-US",
                    ELanguage.French => "fr-FR",
                    ELanguage.German => "de-DE",
                    ELanguage.Italian => "it-IT",
                    ELanguage.Spanish => "es-ES",
                    ELanguage.SpanishMexico => "es-MX",
                    ELanguage.Arabic => "ar-AE",
                    ELanguage.Japanese => "ja-JP",
                    ELanguage.Korean => "ko-KR",
                    ELanguage.Polish => "pl-PL",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru-RU",
                    ELanguage.Turkish => "tr-TR",
                    ELanguage.Chinese => "zh-CN",
                    ELanguage.TraditionalChinese => "zh-TW",
                    ELanguage.Indonesian => "id-ID",
                    ELanguage.Thai => "th-TH",
                    ELanguage.VietnameseVietnam => "vi-VN",
                    _ => "en-US"
                },
                "stateofdecay2" => language switch
                {
                    ELanguage.English => "en-US",
                    ELanguage.AustralianEnglish => "en-AU",
                    ELanguage.French => "fr-FR",
                    ELanguage.German => "de-DE",
                    ELanguage.Italian => "it-IT",
                    ELanguage.SpanishMexico => "es-MX",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru-RU",
                    ELanguage.Chinese => "zh-CN",
                    _ => "en-US"
                },
                "oakgame" => language switch
                {
                    ELanguage.English => "en",
                    ELanguage.French => "fr",
                    ELanguage.German => "de",
                    ELanguage.Italian => "it",
                    ELanguage.Spanish => "es",
                    ELanguage.Japanese => "ja",
                    ELanguage.Korean => "ko",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru",
                    ELanguage.Chinese => "zh-Hans-CN",
                    ELanguage.TraditionalChinese => "zh-Hant-TW",
                    _ => "en"
                },
                "multiversus" => language switch
                {
                    ELanguage.English => "en",
                    ELanguage.French => "fr",
                    ELanguage.German => "de",
                    ELanguage.Italian => "it",
                    ELanguage.Spanish => "es",
                    ELanguage.SpanishLatin => "es-419",
                    ELanguage.Polish => "pl",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru",
                    ELanguage.Chinese => "zh-Hans",
                    _ => "en"
                },
                _ => language switch // https://www.alchemysoftware.com/livedocs/ezscript/Topics/Catalyst/Language.htm
                {
                    ELanguage.English => "en",
                    ELanguage.AustralianEnglish => "en-AU",
                    ELanguage.BritishEnglish => "en-GB",
                    ELanguage.French => "fr",
                    ELanguage.German => "de",
                    ELanguage.Italian => "it",
                    ELanguage.Spanish => "es",
                    ELanguage.SpanishLatin => "es-419",
                    ELanguage.SpanishMexico => "es-MX",
                    ELanguage.Arabic => "ar",
                    ELanguage.Japanese => "ja",
                    ELanguage.Korean => "ko",
                    ELanguage.Polish => "pl",
                    ELanguage.Portuguese => "pt",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru",
                    ELanguage.Turkish => "tr",
                    ELanguage.Chinese => "zh",
                    ELanguage.TraditionalChinese => "zh-Hant",
                    ELanguage.Swedish => "sv",
                    ELanguage.Thai => "th",
                    ELanguage.Indonesian => "id",
                    ELanguage.VietnameseVietnam => "vi-VN",
                    ELanguage.Zulu => "zu",
                    _ => "en"
                }
            };
        }

        public int LoadVirtualPaths() { return LoadVirtualPaths(Versions.Ver); }
        public int LoadVirtualPaths(FPackageFileVersion version, CancellationToken cancellationToken = default)
        {
            var regex = new Regex($"^{ProjectName}/Plugins/.+.upluginmanifest$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            VirtualPaths.Clear();

            var i = 0;
            var useIndividualPlugin = version < EUnrealEngineObjectUE4Version.ADDED_SOFT_OBJECT_PATH || !Files.Any(file => file.Key.EndsWith(".upluginmanifest"));
            foreach ((string filePath, GameFile gameFile) in Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (useIndividualPlugin) // < 4.18 or no .upluginmanifest
                {
                    if (!filePath.EndsWith(".uplugin")) continue;
                    if (!TryCreateReader(gameFile.Path, out var stream)) continue;
                    using var reader = new StreamReader(stream);
                    var pluginFile = JsonConvert.DeserializeObject<UPluginDescriptor>(reader.ReadToEnd());
                    if (!pluginFile!.CanContainContent) continue;
                    var virtPath = gameFile.Path.SubstringAfterLast('/').SubstringBeforeLast('.');
                    var path = gameFile.Path.SubstringBeforeLast('/');

                    if (!VirtualPaths.ContainsKey(virtPath))
                    {
                        VirtualPaths.Add(virtPath, path);
                        i++; // Only increment if we don't have the path already
                    }
                    else
                    {
                        VirtualPaths[virtPath] = path;
                    }
                }
                else
                {
                    if (!regex.IsMatch(filePath)) continue;
                    if (!TryCreateReader(gameFile.Path, out var stream)) continue;
                    using var reader = new StreamReader(stream);
                    var manifest = JsonConvert.DeserializeObject<UPluginManifest>(reader.ReadToEnd());

                    foreach (var content in manifest!.Contents)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!content.Descriptor.CanContainContent) continue;
                        var virtPath = content.File.SubstringAfterLast('/').SubstringBeforeLast('.');
                        var path = content.File.Replace("../../../", string.Empty).SubstringBeforeLast('/');

                        if (!VirtualPaths.ContainsKey(virtPath))
                        {
                            VirtualPaths.Add(virtPath, path);
                            i++; // Only increment if we don't have the path already
                        }
                        else
                        {
                            VirtualPaths[virtPath] = path;
                        }
                    }
                }
            }

            return i;
        }

        protected bool LoadIniConfigs()
        {
            if (TryGetGameFile("/Game/Config/DefaultGame.ini", out var defaultGame))
            {
                if (defaultGame is VfsEntry { Vfs: IAesVfsReader aesVfsReader }) DefaultGame.EncryptionKeyGuid = aesVfsReader.EncryptionKeyGuid;
                if (defaultGame.TryCreateReader(out var gameAr)) DefaultGame.Read(new StreamReader(gameAr));
                gameAr?.Dispose();

                Internationalization.InitFromIni(DefaultGame);
            }
            if (TryGetGameFile("/Game/Config/DefaultEngine.ini", out var defaultEngine))
            {
                if (defaultEngine is VfsEntry { Vfs: IAesVfsReader aesVfsReader }) DefaultEngine.EncryptionKeyGuid = aesVfsReader.EncryptionKeyGuid;
                if (defaultEngine.TryCreateReader(out var engineAr)) DefaultEngine.Read(new StreamReader(engineAr));
                engineAr?.Dispose();

                foreach (var token in DefaultEngine.Sections.FirstOrDefault(s => s.Name == "ConsoleVariables")?.Tokens ?? [])
                {
                    if (token is not InstructionToken it) continue;
                    var boolValue = it.Value.Equals("1");

                    switch (it.Key)
                    {
                        case "a.StripAdditiveRefPose":
                        case "r.StaticMesh.KeepMobileMinLODSettingOnDesktop":
                        case "r.SkeletalMesh.KeepMobileMinLODSettingOnDesktop":
                            Versions[it.Key[2..]] = boolValue;
                            continue;
                    }
                }
            }
            return DefaultGame.Sections.Any(x => x.Name == "/Script/EngineSettings.GeneralProjectSettings");
        }

        public string FixPath(string path)
        {
            path = path.Replace('\\', '/');
            if (path[0] == '/') path = path[1..]; // remove leading slash

            var lastPart = path.SubstringAfterLast('/');
            // This part is only for FSoftObjectPaths and not really needed anymore internally, but it's still in here for user input
            if (lastPart.Contains('.') && lastPart.SubstringBefore('.') == lastPart.SubstringAfter('.'))
                path = string.Concat(path.SubstringBeforeWithLast('/'), lastPart.SubstringBefore('.'));
            if (path[^1] != '/' && !lastPart.Contains('.'))
                path += "." + GameFile.UePackageExtensions[0]; // uasset

            var ret = path;
            var root = path.SubstringBefore('/');
            var tree = path.SubstringAfter('/');
            if (PathComparer.Equals(root, "Game") || PathComparer.Equals(root, "Engine"))
            {
                var projectName = PathComparer.Equals(root, "Engine") ? "Engine" : ProjectName;
                var root2 = tree.SubstringBefore('/');
                if (PathComparer.Equals(root2, "Config") ||
                    PathComparer.Equals(root2, "Content") ||
                    PathComparer.Equals(root2, "Plugins"))
                {
                    ret = string.Concat(projectName, '/', tree);
                }
                else
                {
                    ret = string.Concat(projectName, "/Content/", tree);
                }
            }
            else if (PathComparer.Equals(root, ProjectName))
            {
                // everything should be good
            }
            else if (VirtualPaths.TryGetValue(root, out var use))
            {
                ret = string.Concat(use, "/Content/", tree);
            }
            else if (PathComparer.Equals(ProjectName, "FortniteGame"))
            {
                ret = string.Concat(ProjectName, $"/Plugins/GameFeatures/{root}/Content/", tree);
            }

            return ret;
        }

        #region SaveAsset Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] SaveAsset(string path) => SaveAsset(this[path]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] SaveAsset(GameFile file) => file.Read();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<byte[]> SaveAssetAsync(string path) => SaveAssetAsync(this[path]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<byte[]> SaveAssetAsync(GameFile file)
            => await file.ReadAsync().ConfigureAwait(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySaveAsset(string path, [MaybeNullWhen(false)] out byte[] data)
        {
            if (TryGetGameFile(path, out var file))
            {
                return TrySaveAsset(file, out data);
            }

            data = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySaveAsset(GameFile file, [MaybeNullWhen(false)] out byte[] data)
        {
            data = file.SafeRead();
            return data != null;
        }
        #endregion

        #region CreateReader Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FArchive CreateReader(string path) => this[path].CreateReader();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<FArchive> CreateReaderAsync(string path) => this[path].CreateReaderAsync();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCreateReader(string path, [MaybeNullWhen(false)] out FArchive reader)
        {
            reader = null;
            if (TryGetGameFile(path, out var file))
            {
                reader = file.SafeCreateReader();
            }

            return reader != null;
        }
        #endregion

        #region LoadPackage Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPackage LoadPackage(string path) => LoadPackage(this[path]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPackage LoadPackage(GameFile file) => LoadPackageAsync(file).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<IPackage> LoadPackageAsync(string path) => LoadPackageAsync(this[path]);

        public async Task<IPackage> LoadPackageAsync(GameFile file)
        {
            if (!file.IsUePackage) throw new ArgumentException("cannot load non-UE package", nameof(file));
            Files.FindPayloads(file, out var uexp, out var ubulks, out var uptnls);

            var uasset = await file.CreateReaderAsync().ConfigureAwait(false);
            var lazyUbulk = ubulks.Count > 0 ? new Lazy<FArchive?>(() => ubulks[0].TryCreateReader(out var reader) ? reader : null) : null;
            var lazyUptnl = uptnls.Count > 0 ? new Lazy<FArchive?>(() => uptnls[0].TryCreateReader(out var reader) ? reader : null) : null;

            switch (file)
            {
                case FPakEntry or OsGameFile:
                    var uexpAr = uexp != null ? await uexp.CreateReaderAsync().ConfigureAwait(false) : null;
                    return new Package(uasset, uexpAr, lazyUbulk, lazyUptnl, this, UseLazyPackageSerialization);
                case FIoStoreEntry ioStoreEntry when this is IVfsFileProvider vfsFileProvider:
                    return new IoPackage(uasset, ioStoreEntry.IoStoreReader.ContainerHeader, lazyUbulk, lazyUptnl, vfsFileProvider);
                default:
                    throw new NotImplementedException($"type {file.GetType()} is not supported");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoadPackage(string path, [MaybeNullWhen(false)] out IPackage package)
        {
            if (TryGetGameFile(path, out var file))
            {
                return TryLoadPackage(file, out package);
            }

            package = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoadPackage(GameFile file, [MaybeNullWhen(false)] out IPackage package)
        {
            try
            {
                package = LoadPackage(file);
            }
            catch
            {
                package = null;
            }
            return package != null;
        }
        #endregion

        #region SavePackage Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyDictionary<string, byte[]> SavePackage(string path) => SavePackage(this[path]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyDictionary<string, byte[]> SavePackage(GameFile file) => SavePackageAsync(file).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<IReadOnlyDictionary<string, byte[]>> SavePackageAsync(string path)
            => await SavePackageAsync(this[path]).ConfigureAwait(false);

        public async Task<IReadOnlyDictionary<string, byte[]>> SavePackageAsync(GameFile file)
        {
            Files.FindPayloads(file, out var uexp, out var ubulks, out var uptnls, true);

            var dict = new Dictionary<string, byte[]> { { file.Path, await file.ReadAsync().ConfigureAwait(false) } };
            if (uexp != null && uexp.TryRead(out var uexpData)) dict[uexp.Path] = uexpData;

            foreach (var ubulk in ubulks)
                if (ubulk.TryRead(out var ubulkData))
                    dict[ubulk.Path] = ubulkData;

            foreach (var uptnl in uptnls)
                if (uptnl.TryRead(out var uptnlData))
                    dict[uptnl.Path] = uptnlData;

            return dict;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySavePackage(string path, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, byte[]> data)
        {
            if (TryGetGameFile(path, out var file))
            {
                return TrySavePackage(file, out data);
            }

            data = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySavePackage(GameFile file, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, byte[]> data)
        {
            try
            {
                data = SavePackage(file);
            }
            catch
            {
                data = null;
            }
            return data != null;
        }
        #endregion

        #region LoadObject Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UObject LoadPackageObject(string path) => LoadPackageObject<UObject>(path);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T LoadPackageObject<T>(string path) where T : UObject => LoadPackageObjectAsync<T>(path).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UObject> LoadPackageObjectAsync(string path)
            => await LoadPackageObjectAsync<UObject>(path).ConfigureAwait(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T> LoadPackageObjectAsync<T>(string path) where T : UObject
        {
            var index = path.LastIndexOf('.');

            string objectName;
            if (index == -1)
            {
                objectName = path.SubstringAfterLast('/');
            }
            else
            {
                objectName = path[(index + 1)..];
                path = path[..index];
            }

            return await LoadPackageObjectAsync<T>(path, objectName).ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UObject> LoadPackageObjectAsync(string path, string objectName)
            => await LoadPackageObjectAsync<UObject>(path, objectName).ConfigureAwait(false);

        public async Task<T> LoadPackageObjectAsync<T>(string path, string objectName) where T : UObject
        {
            ArgumentException.ThrowIfNullOrEmpty(nameof(path), path);
            ArgumentException.ThrowIfNullOrEmpty(nameof(objectName), objectName);

            var package = await LoadPackageAsync(path).ConfigureAwait(false);
            return package.GetExport<T>(objectName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoadPackageObject(string path, [MaybeNullWhen(false)] out UObject export)
            => TryLoadPackageObject<UObject>(path, out export);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoadPackageObject<T>(string path, [MaybeNullWhen(false)] out T export) where T : UObject
        {
            try
            {
                export = LoadPackageObject<T>(path);
            }
            catch
            {
                export = null;
            }
            return export != null;
        }

        [Obsolete("use LoadPackage().GetExports() instead")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<UObject> LoadPackageObjects(string path) => LoadPackageObjectsAsync(path).Result;

        [Obsolete("use LoadPackageAsync().GetExports() instead")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<IEnumerable<UObject>> LoadPackageObjectsAsync(string path)
        {
            var package = await LoadPackageAsync(path).ConfigureAwait(false);
            return package.GetExports();
        }
        #endregion

        public virtual void Dispose()
        {
            Files.Clear();
            VirtualPaths.Clear();
            Internationalization.Clear();
        }
    }
}
