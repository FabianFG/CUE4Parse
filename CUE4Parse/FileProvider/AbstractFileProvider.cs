using System;
using System.Collections.Generic;
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
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Localization;
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
        public FGuid EncryptionKeyGuid { get; set; }

        public CustomConfigIni(string name) : base(name) { }
    }

    public abstract class AbstractFileProvider : IFileProvider
    {
        protected static readonly ILogger Log = Serilog.Log.ForContext<IFileProvider>();

        public VersionContainer Versions { get; set; }
        public CustomConfigIni DefaultGame { get; set; }
        public CustomConfigIni DefaultEngine { get; set; }
        public virtual ITypeMappingsProvider? MappingsContainer { get; set; }
        public virtual TypeMappings? MappingsForGame => MappingsContainer?.MappingsForGame;
        public virtual IDictionary<string, IDictionary<string, string>> LocalizedResources { get; } = new Dictionary<string, IDictionary<string, string>>();
        public Dictionary<string, string> VirtualPaths { get; } = new(StringComparer.OrdinalIgnoreCase);
        public abstract IReadOnlyDictionary<string, GameFile> Files { get; }
        public abstract IReadOnlyDictionary<FPackageId, GameFile> FilesById { get; }
        public virtual bool IsCaseInsensitive { get; } // fabian? is this reversed?
        public bool ReadScriptData { get; set; }
        public bool ReadShaderMaps { get; set; }
        public bool SkipReferencedTextures { get; set; }
        public bool UseLazySerialization { get; set; } = true;

        protected AbstractFileProvider(bool isCaseInsensitive = false, VersionContainer? versions = null)
        {
            IsCaseInsensitive = isCaseInsensitive;
            Versions = versions ?? VersionContainer.DEFAULT_VERSION_CONTAINER;
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

                                var stringTable =  Task.Run(() => this.LoadObject<UStringTable>(stringTablePath)).Result;
                                if (stringTable != null)
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

        private string? _internalGameName;
        public string InternalGameName
        {
            get
            {
                if (string.IsNullOrEmpty(_internalGameName))
                {
                    if (Files.Keys.FirstOrDefault(it => it.EndsWith(".uproject", StringComparison.OrdinalIgnoreCase)) is not { } t)
                        t = Files.Keys.FirstOrDefault(
                            it => !it.StartsWith('/') && it.Contains('/') &&
                                  !it.SubstringBefore('/').EndsWith("Engine", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
                    _internalGameName = t.SubstringBefore('/');
                    if (_internalGameName == "midnightsuns") _internalGameName = "codagame";
                }
                return _internalGameName;
            }
        }

        public int LoadLocalization(ELanguage language = ELanguage.English, CancellationToken cancellationToken = default)
        {
            var regex = new Regex($"^{InternalGameName}/.+/{GetLanguageCode(language)}/.+.locres$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            LocalizedResources.Clear();

            var i = 0;
            foreach (var file in Files.Where(x => regex.IsMatch(x.Key)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!file.Value.TryCreateReader(out var archive)) continue;

                var locres = new FTextLocalizationResource(archive);
                foreach (var entries in locres.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!LocalizedResources.ContainsKey(entries.Key.Str))
                        LocalizedResources[entries.Key.Str] = new Dictionary<string, string>();

                    foreach (var keyValue in entries.Value)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        LocalizedResources[entries.Key.Str][keyValue.Key.Str] = keyValue.Value.LocalizedString;
                        i++;
                    }
                }
            }
            return i;
        }

        public virtual string GetLocalizedString(string namespacee, string key, string? defaultValue)
        {
            if (LocalizedResources.TryGetValue(namespacee, out var keyValue) &&
                keyValue.TryGetValue(key, out var localizedResource))
                return localizedResource;

            return defaultValue ?? string.Empty;
        }
        public string GetLanguageCode(ELanguage language)
        {
            return InternalGameName.ToLowerInvariant() switch
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
            var regex = new Regex($"^{InternalGameName}/Plugins/.+.upluginmanifest$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
            if (TryFindGameFile("/Game/Config/DefaultGame.ini", out var defaultGame))
            {
                if (defaultGame is VfsEntry { Vfs: IAesVfsReader aesVfsReader }) DefaultGame.EncryptionKeyGuid = aesVfsReader.EncryptionKeyGuid;
                if (defaultGame.TryCreateReader(out var gameAr)) DefaultGame.Read(new StreamReader(gameAr));
                gameAr?.Dispose();
            }
            if (TryFindGameFile("/Game/Config/DefaultEngine.ini", out var defaultEngine))
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

        public virtual GameFile this[string path] => Files[FixPath(path)];

        public virtual bool TryFindGameFile(string path, out GameFile file)
        {
            var uassetPath = FixPath(path);
            if (Files.TryGetValue(uassetPath, out file))
            {
                return true;
            }

            var umapPath = uassetPath.SubstringBeforeWithLast('.') + GameFile.Ue4PackageExtensions[1];
            if (Files.TryGetValue(umapPath, out file))
            {
                return true;
            }

            return Files.TryGetValue(IsCaseInsensitive ? path.ToLowerInvariant() : path, out file);
        }

        public virtual string FixPath(string path) => FixPath(path, IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        public virtual string FixPath(string path, StringComparison comparisonType)
        {
            path = path.Replace('\\', '/');
            if (path[0] == '/')
                path = path[1..];
            var lastPart = path.SubstringAfterLast('/');
            // This part is only for FSoftObjectPaths and not really needed anymore internally, but it's still in here for user input
            if (lastPart.Contains('.') && lastPart.SubstringBefore('.') == lastPart.SubstringAfter('.'))
                path = string.Concat(path.SubstringBeforeLast('/'), "/", lastPart.SubstringBefore('.'));
            if (path[^1] != '/' && !lastPart.Contains('.'))
                path += "." + GameFile.Ue4PackageExtensions[0];

            var ret = path;
            var root = path.SubstringBefore('/');
            var tree = path.SubstringAfter('/');
            if (root.Equals("Game", comparisonType) || root.Equals("Engine", comparisonType))
            {
                var gameName = root.Equals("Engine", comparisonType) ? "Engine" : InternalGameName;
                var root2 = tree.SubstringBefore('/');
                if (root2.Equals("Config", comparisonType) ||
                    root2.Equals("Content", comparisonType) ||
                    root2.Equals("Plugins", comparisonType))
                {
                    ret = string.Concat(gameName, '/', tree);
                }
                else
                {
                    ret = string.Concat(gameName, "/Content/", tree);
                }
            }
            else if (root.Equals(InternalGameName, StringComparison.OrdinalIgnoreCase))
            {
                // everything should be good
            }
            else if (VirtualPaths.TryGetValue(root, out var use))
            {
                ret = string.Concat(use, "/Content/", tree);
            }
            else if (InternalGameName.Equals("FORTNITEGAME", StringComparison.OrdinalIgnoreCase))
            {
                ret = string.Concat(InternalGameName, $"/Plugins/GameFeatures/{root}/Content/", tree);
            }

            return comparisonType == StringComparison.OrdinalIgnoreCase ? ret.ToLowerInvariant() : ret;
        }

        #region SaveAsset Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual byte[] SaveAsset(string path) => this[path].Read();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TrySaveAsset(string path, out byte[] data)
        {
            if (!TryFindGameFile(path, out var file))
            {
                data = default;
                return false;
            }

            return file.TryRead(out data);
        }

        public virtual async Task<byte[]> SaveAssetAsync(string path) => await Task.Run(() => SaveAsset(path));
        public virtual async Task<byte[]?> TrySaveAssetAsync(string path) => await Task.Run(() =>
        {
            TrySaveAsset(path, out var data);
            return data;
        });

        #endregion

        #region CreateReader Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual FArchive CreateReader(string path) => this[path].CreateReader();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryCreateReader(string path, out FArchive reader)
        {
            if (!TryFindGameFile(path, out var file))
            {
                reader = default;
                return false;
            }

            return file.TryCreateReader(out reader);
        }

        public virtual async Task<FArchive> CreateReaderAsync(string path) => await Task.Run(() => CreateReader(path));
        public virtual async Task<FArchive?> TryCreateReaderAsync(string path) => await Task.Run(() =>
        {
            TryCreateReader(path, out var reader);
            return reader;
        });

        #endregion

        #region LoadPackage Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual IPackage LoadPackage(string path) => LoadPackage(this[path]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual IPackage LoadPackage(GameFile file) => LoadPackageAsync(file).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual IoPackage LoadPackage(FPackageId id) => (IoPackage) LoadPackage(FilesById[id]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryLoadPackage(string path, out IPackage package)
        {
            if (!TryFindGameFile(path, out var file))
            {
                package = default;
                return false;
            }

            return TryLoadPackage(file, out package);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryLoadPackage(GameFile file, out IPackage package)
        {
            package = TryLoadPackageAsync(file).Result;
            return package != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryLoadPackage(FPackageId id, out IoPackage package)
        {
            if (FilesById.TryGetValue(id, out var file) && TryLoadPackage(file, out IPackage loaded))
            {
                package = (IoPackage) loaded;
                return true;
            }

            package = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<IPackage> LoadPackageAsync(string path) => await LoadPackageAsync(this[path]);
        public virtual async Task<IPackage> LoadPackageAsync(GameFile file)
        {
            if (!file.IsUE4Package) throw new ArgumentException("File must be a package to be a loaded as one", nameof(file));
            Files.TryGetValue(file.PathWithoutExtension + ".uexp", out var uexpFile);
            Files.TryGetValue(file.PathWithoutExtension + ".ubulk", out var ubulkFile);
            Files.TryGetValue(file.PathWithoutExtension + ".uptnl", out var uptnlFile);
            var uassetTask = file.CreateReaderAsync();
            var uexpTask = uexpFile?.CreateReaderAsync();
            var ubulkTask = ubulkFile?.CreateReaderAsync();
            var uptnlTask = uptnlFile?.CreateReaderAsync();
            var uasset = await uassetTask;
            var uexp = uexpTask != null ? await uexpTask : null;
            var ubulk = ubulkTask != null ? await ubulkTask : null;
            var uptnl = uptnlTask != null ? await uptnlTask : null;

            if (file is FPakEntry or OsGameFile)
            {
                return new Package(uasset, uexp, ubulk, uptnl, this, MappingsForGame, UseLazySerialization);
            }

            if (this is not IVfsFileProvider vfsFileProvider || vfsFileProvider.GlobalData == null)
            {
                throw new ParserException("Found IoStore Package but global data is missing, can't serialize");
            }

            var containerHeader = ((FIoStoreEntry) file).IoStoreReader.ContainerHeader;
            return new IoPackage(uasset, vfsFileProvider.GlobalData, containerHeader, ubulk, uptnl, this, MappingsForGame);
        }

        public virtual async Task<IPackage?> TryLoadPackageAsync(string path)
        {
            if (!TryFindGameFile(path, out var file))
            {
                return null;
            }

            return await TryLoadPackageAsync(file).ConfigureAwait(false);
        }

        public virtual async Task<IPackage?> TryLoadPackageAsync(GameFile file)
        {
            if (!file.IsUE4Package)
                return null;
            Files.TryGetValue(file.PathWithoutExtension + ".uexp", out var uexpFile);
            Files.TryGetValue(file.PathWithoutExtension + ".ubulk", out var ubulkFile);
            Files.TryGetValue(file.PathWithoutExtension + ".uptnl", out var uptnlFile);
            var uassetTask = file.TryCreateReaderAsync().ConfigureAwait(false);
            var uexpTask = uexpFile?.TryCreateReaderAsync().ConfigureAwait(false);
            var lazyUbulk = ubulkFile != null ? new Lazy<FArchive?>(() => ubulkFile.TryCreateReader(out var reader) ? reader : null) : null;
            var lazyUptnl = uptnlFile != null ? new Lazy<FArchive?>(() => uptnlFile.TryCreateReader(out var reader) ? reader : null) : null;
            var uasset = await uassetTask;
            if (uasset == null)
                return null;
            var uexp = uexpTask != null ? await uexpTask.Value : null;

            try
            {
                if (file is FPakEntry or OsGameFile)
                {
                    return new Package(uasset, uexp, lazyUbulk, lazyUptnl, this, MappingsForGame, UseLazySerialization);
                }

                if (file is FIoStoreEntry ioStoreEntry)
                {
                    var globalData = ((IVfsFileProvider) this).GlobalData;
                    return globalData != null ? new IoPackage(uasset, globalData, ioStoreEntry.IoStoreReader.ContainerHeader, lazyUbulk, lazyUptnl, this, MappingsForGame) : null;
                }

                return null;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to load package " + file);
                return null;
            }
        }

        #endregion

        #region SavePackage Methods

        public virtual IReadOnlyDictionary<string, byte[]> SavePackage(string path) => SavePackage(this[path]);

        public virtual IReadOnlyDictionary<string, byte[]> SavePackage(GameFile file) => SavePackageAsync(file).Result;

        public virtual bool TrySavePackage(string path, out IReadOnlyDictionary<string, byte[]> package)
        {
            if (!TryFindGameFile(path, out var file))
            {
                package = default;
                return false;
            }

            return TrySavePackage(file, out package);
        }

        public virtual bool TrySavePackage(GameFile file, out IReadOnlyDictionary<string, byte[]> package)
        {
            package = TrySavePackageAsync(file).Result;
            return package != null;
        }

        public virtual async Task<IReadOnlyDictionary<string, byte[]>> SavePackageAsync(string path) =>
            await SavePackageAsync(this[path]);

        public virtual async Task<IReadOnlyDictionary<string, byte[]>> SavePackageAsync(GameFile file)
        {
            Files.TryGetValue(file.PathWithoutExtension + ".uexp", out var uexpFile);
            Files.TryGetValue(file.PathWithoutExtension + ".ubulk", out var ubulkFile);
            Files.TryGetValue(file.PathWithoutExtension + ".uptnl", out var uptnlFile);
            var uassetTask = file.ReadAsync();
            var uexpTask = uexpFile?.ReadAsync();
            var ubulkTask = ubulkFile?.ReadAsync();
            var uptnlTask = uptnlFile?.ReadAsync();
            var dict = new Dictionary<string, byte[]>
            {
                { file.Path, await uassetTask }
            };
            var uexp = uexpTask != null ? await uexpTask : null;
            var ubulk = ubulkTask != null ? await ubulkTask : null;
            var uptnl = uptnlTask != null ? await uptnlTask : null;
            if (uexpFile != null && uexp != null)
                dict[uexpFile.Path] = uexp;
            if (ubulkFile != null && ubulk != null)
                dict[ubulkFile.Path] = ubulk;
            if (uptnlFile != null && uptnl != null)
                dict[uptnlFile.Path] = uptnl;
            return dict;
        }

        public virtual async Task<IReadOnlyDictionary<string, byte[]>?> TrySavePackageAsync(string path)
        {
            if (!TryFindGameFile(path, out var file))
            {
                return null;
            }

            return await TrySavePackageAsync(file).ConfigureAwait(false);
        }

        public virtual async Task<IReadOnlyDictionary<string, byte[]>?> TrySavePackageAsync(GameFile file)
        {
            Files.TryGetValue(file.PathWithoutExtension + ".uexp", out var uexpFile);
            Files.TryGetValue(file.PathWithoutExtension + ".ubulk", out var ubulkFile);
            Files.TryGetValue(file.PathWithoutExtension + ".uptnl", out var uptnlFile);
            var uassetTask = file.TryReadAsync().ConfigureAwait(false);
            var uexpTask = uexpFile?.TryReadAsync().ConfigureAwait(false);
            var ubulkTask = ubulkFile?.TryReadAsync().ConfigureAwait(false);
            var uptnlTask = uptnlFile?.TryReadAsync().ConfigureAwait(false);

            var uasset = await uassetTask;
            if (uasset == null)
                return null;
            var uexp = uexpTask != null ? await uexpTask.Value : null;
            var ubulk = ubulkTask != null ? await ubulkTask.Value : null;
            var uptnl = uptnlTask != null ? await uptnlTask.Value : null;

            var dict = new Dictionary<string, byte[]>
            {
                { file.Path, uasset }
            };
            if (uexpFile != null && uexp != null)
                dict[uexpFile.Path] = uexp;
            if (ubulkFile != null && ubulk != null)
                dict[ubulkFile.Path] = ubulk;
            if (uptnlFile != null && uptnl != null)
                dict[uptnlFile.Path] = uptnl;
            return dict;
        }

        #endregion

        #region LoadObject Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual UObject LoadObject(string? objectPath) => LoadObjectAsync(objectPath).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryLoadObject(string? objectPath, out UObject export)
        {
            export = TryLoadObjectAsync(objectPath).Result;
            return export != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual T LoadObject<T>(string? objectPath) where T : UObject => LoadObjectAsync<T>(objectPath).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryLoadObject<T>(string? objectPath, out T export) where T : UObject
        {
            export = TryLoadObjectAsync<T>(objectPath).Result;
            return export != null;
        }

        public virtual async Task<UObject> LoadObjectAsync(string? objectPath)
        {
            if (objectPath == null) throw new ArgumentException("ObjectPath can't be null", nameof(objectPath));
            var packagePath = objectPath;
            string objectName;
            var dotIndex = packagePath.IndexOf('.');
            if (dotIndex == -1) // use the package name as object name
            {
                objectName = packagePath.SubstringAfterLast('/');
            }
            else // packagePath.objectName
            {
                objectName = packagePath.Substring(dotIndex + 1);
                packagePath = packagePath.Substring(0, dotIndex);
            }

            var pkg = await LoadPackageAsync(packagePath);
            return pkg.GetExport(objectName, IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        public virtual async Task<UObject?> TryLoadObjectAsync(string? objectPath)
        {
            if (objectPath == null || objectPath.Equals("None", IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) return null;
            var packagePath = objectPath;
            string objectName;
            var dotIndex = packagePath.IndexOf('.');
            if (dotIndex == -1) // use the package name as object name
            {
                objectName = packagePath.SubstringAfterLast('/');
            }
            else // packagePath.objectName
            {
                objectName = packagePath.Substring(dotIndex + 1);
                packagePath = packagePath.Substring(0, dotIndex);
            }

            var pkg = await TryLoadPackageAsync(packagePath).ConfigureAwait(false);
            return pkg?.GetExportOrNull(objectName, IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<T> LoadObjectAsync<T>(string? objectPath) where T : UObject =>
            await LoadObjectAsync(objectPath) as T ??
            throw new ParserException("Loaded object but it was of wrong type");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<T?> TryLoadObjectAsync<T>(string? objectPath) where T : UObject =>
            await TryLoadObjectAsync(objectPath) as T;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual IEnumerable<UObject> LoadAllObjects(string? packagePath) => LoadAllObjectsAsync(packagePath).GetAwaiter().GetResult();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<IEnumerable<UObject>> LoadAllObjectsAsync(string? packagePath)
        {
            if (packagePath == null) throw new ArgumentException("PackagePath can't be null", nameof(packagePath));

            var pkg = await LoadPackageAsync(packagePath);
            return pkg.GetExports();
        }

        #endregion
    }
}
