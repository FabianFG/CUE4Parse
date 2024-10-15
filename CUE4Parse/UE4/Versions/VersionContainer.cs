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

        private EGame _game;
        public EGame Game
        {
            get => _game;
            set
            {
                _game = value;
                InitOptions();
                InitMapStructTypes();
            }
        }

        private FPackageFileVersion _ver;
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
                InitMapStructTypes();
            }
        }

        public bool bExplicitVer { get; private set; }

        public FCustomVersionContainer? CustomVersions;
        public readonly Dictionary<string, bool> Options = new();
        public readonly Dictionary<string, KeyValuePair<string, string>> MapStructTypes = new();

        private readonly IDictionary<string, bool>? _optionOverrides;
        private readonly IDictionary<string, KeyValuePair<string, string>>? _mapStructTypesOverrides;

        public VersionContainer(EGame game = GAME_UE4_LATEST, ETexturePlatform platform = ETexturePlatform.DesktopMobile, FPackageFileVersion ver = default, FCustomVersionContainer? customVersions = null, IDictionary<string, bool>? optionOverrides = null, IDictionary<string, KeyValuePair<string, string>>? mapStructTypesOverrides = null)
        {
            _optionOverrides = optionOverrides;
            _mapStructTypesOverrides = mapStructTypesOverrides;

            Game = game;
            Ver = ver;
            Platform = platform;
            CustomVersions = customVersions;
        }

        private void InitOptions()
        {
            Options.Clear();

            // objects
            Options["MorphTarget"] = true;

            // structs
            Options["Vector_NetQuantize_AsStruct"] = false;

            // fields
            Options["RawIndexBuffer.HasShouldExpandTo32Bit"] = Game >= GAME_UE4_25;
            Options["ShaderMap.UseNewCookedFormat"] = Game >= GAME_UE5_0;
            Options["SkeletalMesh.UseNewCookedFormat"] = Game >= GAME_UE4_24;
            Options["SkeletalMesh.HasRayTracingData"] = Game is >= GAME_UE4_27 or GAME_UE4_25_Plus;
            Options["StaticMesh.HasLODsShareStaticLighting"] = Game is < GAME_UE4_15 or >= GAME_UE4_16; // Exists in all engine versions except UE4.15
            Options["StaticMesh.HasRayTracingGeometry"] = Game >= GAME_UE4_25;
            Options["StaticMesh.HasVisibleInRayTracing"] = Game >= GAME_UE4_26;
            Options["StaticMesh.UseNewCookedFormat"] = Game >= GAME_UE4_23;
            Options["VirtualTextures"] = Game >= GAME_UE4_23;
            Options["SoundWave.UseAudioStreaming"] = Game >= GAME_UE4_25 && Game != GAME_UE4_28 && Game != GAME_GTATheTrilogyDefinitiveEdition && Game != GAME_ReadyOrNot && Game != GAME_BladeAndSoul; // A lot of games use this, but some don't, which causes issues.
            Options["AnimSequence.HasCompressedRawSize"] = Game >= GAME_UE4_17; // Early 4.17 builds don't have this, and some custom engine builds don't either.
            Options["StaticMesh.HasNavCollision"] = Ver >= EUnrealEngineObjectUE4Version.STATIC_MESH_STORE_NAV_COLLISION && Game != GAME_GearsOfWar4 && Game != GAME_TEKKEN7;

            // special general property workarounds
            Options["ByteProperty.TMap64Bit"] = false;
            Options["ByteProperty.TMap16Bit"] = false;
            Options["ByteProperty.TMap8Bit"] = false;

            // defaults
            Options["StripAdditiveRefPose"] = false;
            Options["SkeletalMesh.KeepMobileMinLODSettingOnDesktop"] = false;
            Options["StaticMesh.KeepMobileMinLODSettingOnDesktop"] = false;

            if (_optionOverrides == null) return;
            foreach (var (key, value) in _optionOverrides)
            {
                Options[key] = value;
            }
        }

        private void InitMapStructTypes()
        {
            MapStructTypes.Clear();
            MapStructTypes["BindingIdToReferences"] = new KeyValuePair<string, string>("Guid", null);
            MapStructTypes["UserParameterRedirects"] = new KeyValuePair<string, string>("NiagaraVariable", "NiagaraVariable");
            MapStructTypes["Tracks"] = new KeyValuePair<string, string>("MovieSceneTrackIdentifier", null);
            MapStructTypes["SubSequences"] = new KeyValuePair<string, string>("MovieSceneSequenceID", null);
            MapStructTypes["Hierarchy"] = new KeyValuePair<string, string>("MovieSceneSequenceID", null);
            MapStructTypes["TrackSignatureToTrackIdentifier"] = new KeyValuePair<string, string>("Guid", "MovieSceneTrackIdentifier");
            MapStructTypes["UserParameterRedirects"] = new KeyValuePair<string, string>("NiagaraVariable", "NiagaraVariable");

            if (_mapStructTypesOverrides == null) return;
            foreach (var (key, value) in _mapStructTypesOverrides)
            {
                MapStructTypes[key] = value;
            }
        }

        public bool this[string optionKey]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Options[optionKey];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Options[optionKey] = value;
        }

        public object Clone() => new VersionContainer(Game, Platform, Ver, CustomVersions, _optionOverrides, _mapStructTypesOverrides) { bExplicitVer = bExplicitVer };
    }
}
