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
        public UE4Version Ver;
        public List<FCustomVersion>? CustomVersions;
        public readonly Dictionary<string, bool> Options = new();
        private readonly Dictionary<string, bool>? _optionOverrides;

        public VersionContainer(EGame game = GAME_UE4_LATEST, UE4Version ver = UE4Version.VER_UE4_DETERMINE_BY_GAME, List<FCustomVersion>? customVersions = null, Dictionary<string, bool>? optionOverrides = null)
        {
            _optionOverrides = optionOverrides;
            Game = game;
            Ver = ver == UE4Version.VER_UE4_DETERMINE_BY_GAME ? game.GetVersion() : ver;
            CustomVersions = customVersions;
        }

        private void InitOptions()
        {
            Options.Clear();
            Options["RawIndexBuffer.HasShouldExpandTo32Bit"] = Game >= GAME_UE4_25;
            Options["ShaderMap.UseNewCookedFormat"] = Game >= GAME_UE5_0;
            Options["SkeletalMesh.KeepMobileMinLODSettingOnDesktop"] = Game >= GAME_UE4_27;
            Options["SkeletalMesh.UseNewCookedFormat"] = Game >= GAME_UE4_24;
            Options["StaticMesh.HasRayTracingGeometry"] = Game >= GAME_UE4_25;
            Options["StaticMesh.HasVisibleInRayTracing"] = Game >= GAME_UE4_26;
            Options["StaticMesh.KeepMobileMinLODSettingOnDesktop"] = Game >= GAME_UE4_27;
            Options["StaticMesh.UseNewCookedFormat"] = Game >= GAME_UE4_23;
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

        public object Clone() => new VersionContainer(Game, Ver, CustomVersions, _optionOverrides);
    }
}