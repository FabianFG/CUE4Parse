using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Serialization;

namespace CUE4Parse.UE4.Versions
{
    public class VersionContainer : ICloneable
    {
        public static readonly VersionContainer DEFAULT_VERSION_CONTAINER = new();

        public EGame Game;
        public UE4Version Ver;
        public List<FCustomVersion>? CustomVersions;

        public VersionContainer(EGame game = EGame.GAME_UE4_LATEST, UE4Version ver = UE4Version.VER_UE4_DETERMINE_BY_GAME, List<FCustomVersion>? customVersions = null)
        {
            Game = game;
            Ver = ver == UE4Version.VER_UE4_DETERMINE_BY_GAME ? game.GetVersion() : ver;
            CustomVersions = customVersions;
        }

        public object Clone() => new VersionContainer(Game, Ver, CustomVersions);
    }
}