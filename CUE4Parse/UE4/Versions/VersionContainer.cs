using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Serialization;
using static CUE4Parse.UE4.Versions.EGame;

namespace CUE4Parse.UE4.Versions
{
    public class VersionContainer : ICloneable
    {
        public static readonly VersionContainer DEFAULT_VERSION_CONTAINER = new();

        public EGame Game
        {
            get => _game;
            set
            {
                _game = value;
                InitOptions();
            }
        }
        private EGame _game;
        public FPackageFileVersion Ver
        {
            get => _ver;
            set
            {
                bExplicitVer = value.FileVersionUE4 != 0 || value.FileVersionUE5 != 0;
                _ver = bExplicitVer ? value : _game.GetVersion();
            }
        }

        private ETexturePlatform _platform;
        public ETexturePlatform Platform
        {
            get => _platform;
            set
            {
                _platform = value;
                InitOptions();
            }
        }
        private FPackageFileVersion _ver;
        public bool bExplicitVer { get; private set; }
        public List<FCustomVersion>? CustomVersions;
        public readonly Dictionary<string, bool> Options = new();
        private readonly Dictionary<string, bool>? _optionOverrides;

        public VersionContainer(EGame game = GAME_UE4_LATEST, ETexturePlatform platform = ETexturePlatform.DesktopMobile, FPackageFileVersion ver = default, List<FCustomVersion>? customVersions = null, Dictionary<string, bool>? optionOverrides = null)
        {
            _optionOverrides = optionOverrides;
            Game = game;
            Ver = ver;
            Platform = platform;
            CustomVersions = customVersions;
        }

        private void InitOptions()
        {
            Options.Clear();
            Options["RawIndexBuffer.HasShouldExpandTo32Bit"] = Game >= GAME_UE4_25;
            Options["ShaderMap.UseNewCookedFormat"] = Game >= GAME_UE5_0;
            Options["SkeletalMesh.KeepMobileMinLODSettingOnDesktop"] = Game >= GAME_UE5_0;
            Options["SkeletalMesh.UseNewCookedFormat"] = Game >= GAME_UE4_24;
            Options["SkeletalMesh.HasRayTracingData"] = Game is >= GAME_UE4_27 or GAME_UE4_25_Plus;
            Options["StaticMesh.HasLODsShareStaticLighting"] = Game is < GAME_UE4_15 or >= GAME_UE4_16; // Exists in all engine versions except UE4.15
            Options["StaticMesh.HasRayTracingGeometry"] = Game >= GAME_UE4_25;
            Options["StaticMesh.HasVisibleInRayTracing"] = Game >= GAME_UE4_26;
            Options["StaticMesh.KeepMobileMinLODSettingOnDesktop"] = Game >= GAME_UE5_0;
            Options["StaticMesh.UseNewCookedFormat"] = Game >= GAME_UE4_23;
            Options["VirtualTextures"] = Game >= GAME_UE4_23;
            Options["SoundWave.UseAudioStreaming"] = Game >= GAME_UE4_25 && Game != GAME_GTATheTrilogyDefinitiveEdition && Game != GAME_ReadyOrNot; // A lot of games use this, but some don't, which causes issues.

            if (_optionOverrides != null)
            {
                foreach ((string key, bool value) in _optionOverrides)
                {
                    Options[key] = value;
                }
            }
        }

        public bool this[string optionKey]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Options[optionKey];
        }

        public object Clone() => new VersionContainer(Game, Platform, Ver, CustomVersions, _optionOverrides) { bExplicitVer = bExplicitVer };
    }
}
