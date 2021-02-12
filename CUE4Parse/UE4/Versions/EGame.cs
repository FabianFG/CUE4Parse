using System;
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
        GAME_UE4_11 = GameUtils.GameUe4Base + 11 << 4,
        GAME_UE4_12 = GameUtils.GameUe4Base + 12 << 4,
        GAME_UE4_13 = GameUtils.GameUe4Base + 13 << 4,
        GAME_UE4_14 = GameUtils.GameUe4Base + 14 << 4,
        GAME_UE4_15 = GameUtils.GameUe4Base + 15 << 4,
        GAME_UE4_16 = GameUtils.GameUe4Base + 16 << 4,
        GAME_UE4_17 = GameUtils.GameUe4Base + 17 << 4,
        GAME_UE4_18 = GameUtils.GameUe4Base + 18 << 4,
        GAME_UE4_19 = GameUtils.GameUe4Base + 19 << 4,
        GAME_UE4_20 = GameUtils.GameUe4Base + 20 << 4,
        GAME_UE4_21 = GameUtils.GameUe4Base + 21 << 4,
        GAME_UE4_22 = GameUtils.GameUe4Base + 22 << 4,
        GAME_UE4_23 = GameUtils.GameUe4Base + 23 << 4,
        GAME_UE4_24 = GameUtils.GameUe4Base + 24 << 4,
            GAME_VALORANT = GAME_UE4_24 + 1,
        GAME_UE4_25 = GameUtils.GameUe4Base + 25 << 4,
        GAME_UE4_26 = GameUtils.GameUe4Base + 26 << 4,
        GAME_UE4_27 = GameUtils.GameUe4Base + 27 << 4,
        GAME_UE4_LAST,
        GAME_UE4_LATEST = GAME_UE4_LAST - 1
    }
    
    public static class GameUtils
    {
        public const int GameUe4Base = 0x1000000;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GAME_UE4(int x)
        {
            return GameUe4Base + x << 4;
        }

        public static UE4Version GetVersion(this EGame game)
        {
            // Custom UE Games
            // If a game needs a even more specific custom version than the major release version you can add it below
            // if (game == EGame.GAME_VALORANT)
            //     return UE4Version.VER_UE4_24;
            
            // General UE4 Versions
            if (game < EGame.GAME_UE4_1)
                return UE4Version.VER_UE4_0;
            else if (game < EGame.GAME_UE4_2)
                return UE4Version.VER_UE4_1;
            else if (game < EGame.GAME_UE4_3)
                return UE4Version.VER_UE4_2;
            else if (game < EGame.GAME_UE4_4)
                return UE4Version.VER_UE4_3;
            else if (game < EGame.GAME_UE4_5)
                return UE4Version.VER_UE4_4;
            else if (game < EGame.GAME_UE4_6)
                return UE4Version.VER_UE4_5;
            else if (game < EGame.GAME_UE4_7)
                return UE4Version.VER_UE4_6;
            else if (game < EGame.GAME_UE4_8)
                return UE4Version.VER_UE4_7;
            else if (game < EGame.GAME_UE4_9)
                return UE4Version.VER_UE4_8;
            else if (game < EGame.GAME_UE4_10)
                return UE4Version.VER_UE4_9;
            else if (game < EGame.GAME_UE4_11)
                return UE4Version.VER_UE4_10;
            else if (game < EGame.GAME_UE4_12)
                return UE4Version.VER_UE4_11;
            else if (game < EGame.GAME_UE4_13)
                return UE4Version.VER_UE4_12;
            else if (game < EGame.GAME_UE4_14)
                return UE4Version.VER_UE4_13;
            else if (game < EGame.GAME_UE4_15)
                return UE4Version.VER_UE4_14;
            else if (game < EGame.GAME_UE4_16)
                return UE4Version.VER_UE4_15;
            else if (game < EGame.GAME_UE4_17)
                return UE4Version.VER_UE4_16;
            else if (game < EGame.GAME_UE4_18)
                return UE4Version.VER_UE4_17;
            else if (game < EGame.GAME_UE4_19)
                return UE4Version.VER_UE4_18;
            else if (game < EGame.GAME_UE4_20)
                return UE4Version.VER_UE4_19;
            else if (game < EGame.GAME_UE4_21)
                return UE4Version.VER_UE4_20;
            else if (game < EGame.GAME_UE4_22)
                return UE4Version.VER_UE4_21;
            else if (game < EGame.GAME_UE4_23)
                return UE4Version.VER_UE4_22;
            else if (game < EGame.GAME_UE4_24)
                return UE4Version.VER_UE4_23;
            else if (game < EGame.GAME_UE4_25)
                return UE4Version.VER_UE4_24;
            else if (game < EGame.GAME_UE4_26)
                return UE4Version.VER_UE4_25;
            else if (game < EGame.GAME_UE4_27)
                return UE4Version.VER_UE4_26;
            else
                return UE4Version.VER_UE4_LATEST;
        }
    }
}