using System.Runtime.CompilerServices;

namespace CUE4Parse.UE4.Versions
{
    public enum EGame
    {
        // bytes: 01.00.0N.NX : 01=UE4, 00=masked by GAME_ENGINE, NN=UE4 subversion, X=game (4 bits, 0=base engine)
        GAME_UE4_0 = GameUtils.GameUe4Base + 0 << 4,
        GAME_UE4_1 = GameUtils.GameUe4Base + 1 << 4,
        GAME_UE4_2 = GameUtils.GameUe4Base + 2 << 4,
        GAME_UE4_3 = GameUtils.GameUe4Base + 3 << 4,
        GAME_UE4_4 = GameUtils.GameUe4Base + 4 << 4,
        GAME_UE4_5 = GameUtils.GameUe4Base + 5 << 4,
        GAME_UE4_6 = GameUtils.GameUe4Base + 6 << 4,
        GAME_UE4_7 = GameUtils.GameUe4Base + 7 << 4,
        GAME_UE4_8 = GameUtils.GameUe4Base + 8 << 4,
        GAME_UE4_9 = GameUtils.GameUe4Base + 9 << 4,
        GAME_UE4_10 = GameUtils.GameUe4Base + 10 << 4,
            GAME_SeaOfThieves = GAME_UE4_10 + 1,
        GAME_UE4_11 = GameUtils.GameUe4Base + 11 << 4,
        GAME_UE4_12 = GameUtils.GameUe4Base + 12 << 4,
        GAME_UE4_13 = GameUtils.GameUe4Base + 13 << 4,
            GAME_StateOfDecay2 = GAME_UE4_13 + 1,
        GAME_UE4_14 = GameUtils.GameUe4Base + 14 << 4,
        GAME_UE4_15 = GameUtils.GameUe4Base + 15 << 4,
        GAME_UE4_16 = GameUtils.GameUe4Base + 16 << 4,
            GAME_PlayerUnknownsBattlegrounds = GAME_UE4_16 + 1,
        GAME_UE4_17 = GameUtils.GameUe4Base + 17 << 4,
        GAME_UE4_18 = GameUtils.GameUe4Base + 18 << 4,
        GAME_UE4_19 = GameUtils.GameUe4Base + 19 << 4,
        GAME_UE4_20 = GameUtils.GameUe4Base + 20 << 4,
            GAME_Borderlands3 = GAME_UE4_20 + 1,
        GAME_UE4_21 = GameUtils.GameUe4Base + 21 << 4,
            GAME_StarWarsJediFallenOrder = GAME_UE4_21 + 1,
        GAME_UE4_22 = GameUtils.GameUe4Base + 22 << 4,
        GAME_UE4_23 = GameUtils.GameUe4Base + 23 << 4,
        GAME_UE4_24 = GameUtils.GameUe4Base + 24 << 4,
        GAME_UE4_25 = GameUtils.GameUe4Base + 25 << 4,
            GAME_RogueCompany = GAME_UE4_25 + 1,
            GAME_KenaBridgeofSpirits = GAME_UE4_25 + 3,
            GAME_UE4_25_Plus = GAME_UE4_25 + 4,
        GAME_UE4_26 = GameUtils.GameUe4Base + 26 << 4,
            GAME_GTATheTrilogyDefinitiveEdition = GAME_UE4_26 + 1,
            GAME_ReadyOrNot = GAME_UE4_26 + 2,
            GAME_Valorant = GAME_UE4_26 + 3,
            GAME_TowerOfFantasy = GAME_UE4_26 + 4,

        GAME_UE4_27 = GameUtils.GameUe4Base + 27 << 4,
            GAME_Splitgate = GAME_UE4_27 + 1,

        GAME_UE4_LATEST = GAME_UE4_27,

        // TODO Figure out the enum name for UE5 Early Access
        // The commit https://github.com/EpicGames/UnrealEngine/commit/cf116088ae6b65c1701eee99288e43c7310d6bb1#diff-6178e9d97c98e321fc3f53770109ea7f6a8ea7a86cac542717a81922f2f93613R723
        // changed the IoStore and its packages format which breaks backward compatibility with 5.0.0-16433597+++UE5+Release-5.0-EarlyAccess
        GAME_UE5_0 = GameUtils.GameUe5Base + 0 << 4,
        GAME_UE5_1 = GameUtils.GameUe5Base + 1 << 4,
        GAME_UE5_2 = GameUtils.GameUe5Base + 2 << 4,

        GAME_UE5_LATEST = GAME_UE5_1
    }

    public static class GameUtils
    {
        public const int GameUe4Base = 0x1000000;
        public const int GameUe5Base = 0x2000000;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GAME_UE4(int x)
        {
            return GameUe4Base + x << 4;
        }

        public static FPackageFileVersion GetVersion(this EGame game)
        {
            // Custom UE Games
            // If a game needs a even more specific custom version than the major release version you can add it below
            // if (game == EGame.GAME_VALORANT)
            //     return UE4Version.VER_UE4_24;

            if (game >= EGame.GAME_UE5_0)
            {
                return game switch
                {
                    EGame.GAME_UE5_0 => new(522, 1002),
                    EGame.GAME_UE5_1 => new(522, 1007),
                    _ => new((int) EUnrealEngineObjectUE4Version.AUTOMATIC_VERSION, (int) EUnrealEngineObjectUE5Version.AUTOMATIC_VERSION)
                };
            }

            return FPackageFileVersion.CreateUE4Version(game switch
            {
                // General UE4 Versions
                < EGame.GAME_UE4_1 => 342,
                < EGame.GAME_UE4_2 => 352,
                < EGame.GAME_UE4_3 => 363,
                < EGame.GAME_UE4_4 => 382,
                < EGame.GAME_UE4_5 => 385,
                < EGame.GAME_UE4_6 => 401,
                < EGame.GAME_UE4_7 => 413,
                < EGame.GAME_UE4_8 => 434,
                < EGame.GAME_UE4_9 => 451,
                < EGame.GAME_UE4_10 => 482,
                < EGame.GAME_UE4_11 => 482,
                < EGame.GAME_UE4_12 => 498,
                < EGame.GAME_UE4_13 => 504,
                < EGame.GAME_UE4_14 => 505,
                < EGame.GAME_UE4_15 => 508,
                < EGame.GAME_UE4_16 => 510,
                < EGame.GAME_UE4_17 => 513,
                < EGame.GAME_UE4_18 => 513,
                < EGame.GAME_UE4_19 => 514,
                < EGame.GAME_UE4_20 => 516,
                < EGame.GAME_UE4_21 => 516,
                < EGame.GAME_UE4_22 => 517,
                < EGame.GAME_UE4_23 => 517,
                < EGame.GAME_UE4_24 => 517,
                < EGame.GAME_UE4_25 => 518,
                < EGame.GAME_UE4_26 => 518,
                < EGame.GAME_UE4_27 => 522,
                _ => (int) EUnrealEngineObjectUE4Version.AUTOMATIC_VERSION
            });
        }
    }
}
