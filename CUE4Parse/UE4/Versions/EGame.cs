using System.Runtime.CompilerServices;

namespace CUE4Parse.UE4.Versions
{
    public static class GameUtils
    {
        public static readonly int GameUe4Base = 0x1000000;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GAME_UE4(int x)
        {
            return GameUe4Base + x << 4;
        }
    }
    public enum EGame
    {
        // bytes: 01.00.0N.NX : 01=UE4, 00=masked by GAME_ENGINE, NN=UE4 subversion, X=game (4 bits, 0=base engine)
        GAME_UE4_0 = 0x1000000 + 0 << 4,
        GAME_UE4_1 = 0x1000000 + 1 << 4,
        GAME_UE4_2 = 0x1000000 + 2 << 4,
        GAME_UE4_3 = 0x1000000 + 3 << 4,
        GAME_UE4_4 = 0x1000000 + 4 << 4,
        GAME_UE4_5 = 0x1000000 + 5 << 4,
        GAME_UE4_6 = 0x1000000 + 6 << 4,
        GAME_UE4_7 = 0x1000000 + 7 << 4,
        GAME_UE4_8 = 0x1000000 + 8 << 4,
        GAME_UE4_9 = 0x1000000 + 9 << 4,
        GAME_UE4_10 = 0x1000000 + 10 << 4,
        GAME_UE4_11 = 0x1000000 + 11 << 4,
        GAME_UE4_12 = 0x1000000 + 12 << 4,
        GAME_UE4_13 = 0x1000000 + 13 << 4,
        GAME_UE4_14 = 0x1000000 + 14 << 4,
        GAME_UE4_15 = 0x1000000 + 15 << 4,
        GAME_UE4_16 = 0x1000000 + 16 << 4,
        GAME_UE4_17 = 0x1000000 + 17 << 4,
        GAME_UE4_18 = 0x1000000 + 18 << 4,
        GAME_UE4_19 = 0x1000000 + 19 << 4,
        GAME_UE4_20 = 0x1000000 + 20 << 4,
        GAME_UE4_21 = 0x1000000 + 21 << 4,
        GAME_UE4_22 = 0x1000000 + 22 << 4,
        GAME_UE4_23 = 0x1000000 + 23 << 4,
        GAME_UE4_24 = 0x1000000 + 24 << 4,
        GAME_UE4_25 = 0x1000000 + 25 << 4,
        GAME_UE4_26 = 0x1000000 + 26 << 4,
        GAME_UE4_LAST,
        GAME_UE4_LATEST = GAME_UE4_LAST - 1
    }
}