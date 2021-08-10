using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        public UE4Version Ver
        {
            get => _ver;
            set
            {
                bExplicitVer = value != UE4Version.VER_UE4_DETERMINE_BY_GAME;
                _ver = bExplicitVer ? value : _game.GetVersion();
            }
        }
        private UE4Version _ver;
        public bool bExplicitVer { get; private set; } 
        public List<FCustomVersion>? CustomVersions;
        public readonly Dictionary<string, bool> Options = new();
        private readonly Dictionary<string, bool>? _optionOverrides;

        public VersionContainer(EGame game = GAME_UE4_LATEST, UE4Version ver = UE4Version.VER_UE4_DETERMINE_BY_GAME, List<FCustomVersion>? customVersions = null, Dictionary<string, bool>? optionOverrides = null)
        {
            _optionOverrides = optionOverrides;
            Game = game;
            Ver = ver;
            CustomVersions = customVersions;
        }

        private void InitOptions()
        {
            Options.Clear();
            Options["RawIndexBuffer.HasShouldExpandTo32Bit"] = Game >= GAME_UE4_25;
            Options["ShaderMap.UseNewCookedFormat"] = Game >= GAME_UE5_0;
            Options["SkeletalMesh.KeepMobileMinLODSettingOnDesktop"] = Game >= GAME_UE4_27;
            Options["SkeletalMesh.UseNewCookedFormat"] = Game >= GAME_UE4_24;
            Options["StaticMesh.HasLODsShareStaticLighting"] = Game is < GAME_UE4_15 or >= GAME_UE4_16; // Exists in all engine versions except UE4.15
            Options["StaticMesh.HasRayTracingGeometry"] = Game >= GAME_UE4_25;
            Options["StaticMesh.HasVisibleInRayTracing"] = Game >= GAME_UE4_26;
            Options["StaticMesh.KeepMobileMinLODSettingOnDesktop"] = Game >= GAME_UE4_27;
            Options["StaticMesh.UseNewCookedFormat"] = Game >= GAME_UE4_23;
            Options["Texture.64BitSkipOffsets"] = Game >= GAME_UE4_20; // TODO check other occurrences of Game >= GAME_UE4_20 and rename this
            Options["VirtualTextures"] = Game >= GAME_UE4_23;

            if (_optionOverrides != null)
            {
                foreach (var (key, value) in _optionOverrides)
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

        public object Clone() => new VersionContainer(Game, Ver, CustomVersions, _optionOverrides) { bExplicitVer = bExplicitVer };
    }
}