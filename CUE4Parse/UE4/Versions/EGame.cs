using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Versions;

[JsonConverter(typeof(EGameConverter))]
public enum EGame : uint
{
    // bytes: 04.NN.FF.XX : 04/05=UE4/5, NN=UE4 subversion, FF=Flags (curently not used), XX=game (0=base engine)
    GAME_UE4_0 = GameUtils.GameUe4Base + (0 << 16),
    GAME_UE4_1 = GameUtils.GameUe4Base + (1 << 16),
    GAME_UE4_2 = GameUtils.GameUe4Base + (2 << 16),
    GAME_UE4_3 = GameUtils.GameUe4Base + (3 << 16),
    GAME_UE4_4 = GameUtils.GameUe4Base + (4 << 16),
    GAME_UE4_5 = GameUtils.GameUe4Base + (5 << 16),
        GAME_ArkSurvivalEvolved = GAME_UE4_5 + 1,
    GAME_UE4_6 = GameUtils.GameUe4Base + (6 << 16),
    GAME_UE4_7 = GameUtils.GameUe4Base + (7 << 16),
    GAME_UE4_8 = GameUtils.GameUe4Base + (8 << 16),
    GAME_UE4_9 = GameUtils.GameUe4Base + (9 << 16),
    GAME_UE4_10 = GameUtils.GameUe4Base + (10 << 16),
        GAME_SeaOfThieves = GAME_UE4_10 + 1,
    GAME_UE4_11 = GameUtils.GameUe4Base + (11 << 16),
        GAME_GearsOfWar4 = GAME_UE4_11 + 1,
        GAME_DaysGone = GAME_UE4_11 + 2,
    GAME_UE4_12 = GameUtils.GameUe4Base + (12 << 16),
    GAME_UE4_13 = GameUtils.GameUe4Base + (13 << 16),
        GAME_StateOfDecay2 = GAME_UE4_13 + 1,
    GAME_UE4_14 = GameUtils.GameUe4Base + (14 << 16),
        GAME_TEKKEN7 = GAME_UE4_14 + 1,
    GAME_UE4_15 = GameUtils.GameUe4Base + (15 << 16),
    GAME_UE4_16 = GameUtils.GameUe4Base + (16 << 16),
        GAME_PlayerUnknownsBattlegrounds = GAME_UE4_16 + 1,
        GAME_TrainSimWorld2020 = GAME_UE4_16 + 2,
    GAME_UE4_17 = GameUtils.GameUe4Base + (17 << 16),
        GAME_AWayOut = GAME_UE4_17 + 1,
    GAME_UE4_18 = GameUtils.GameUe4Base + (18 << 16),
        GAME_KingdomHearts3 = GAME_UE4_18 + 1,
        GAME_FinalFantasy7Remake = GAME_UE4_18 + 2,
        GAME_AceCombat7 = GAME_UE4_18 + 3,
        GAME_FridayThe13th = GAME_UE4_18 + 4,
        GAME_GameForPeace = GAME_UE4_18 + 5,
    GAME_UE4_19 = GameUtils.GameUe4Base + (19 << 16),
        GAME_Paragon = GAME_UE4_19 + 1,
    GAME_UE4_20 = GameUtils.GameUe4Base + (20 << 16),
        GAME_Borderlands3 = GAME_UE4_20 + 1,
    GAME_UE4_21 = GameUtils.GameUe4Base + (21 << 16),
        GAME_StarWarsJediFallenOrder = GAME_UE4_21 + 1,
        GAME_Undawn = GAME_UE4_21 + 2,
    GAME_UE4_22 = GameUtils.GameUe4Base + (22 << 16),
    GAME_UE4_23 = GameUtils.GameUe4Base + (23 << 16),
        GAME_ApexLegendsMobile = GAME_UE4_23 + 1,
    GAME_UE4_24 = GameUtils.GameUe4Base + (24 << 16),
        GAME_TonyHawkProSkater12 = GAME_UE4_24 + 1,
        GAME_BigRumbleBoxingCreedChampions = GAME_UE4_24 + 2,
    GAME_UE4_25 = GameUtils.GameUe4Base + (25 << 16),
        GAME_UE4_25_Plus = GAME_UE4_25 + 1,
        GAME_RogueCompany = GAME_UE4_25 + 2,
        GAME_DeadIsland2 = GAME_UE4_25 + 3,
        GAME_KenaBridgeofSpirits = GAME_UE4_25 + 4,
        GAME_Strinova = GAME_UE4_25 + 5,
        GAME_SYNCED = GAME_UE4_25 + 6,
        GAME_OperationApocalypse = GAME_UE4_25 + 7,
        GAME_Farlight84 = GAME_UE4_25 + 8,
        GAME_StarWarsHunters = GAME_UE4_25 + 9,
        GAME_ThePathless = GAME_UE4_25 + 10,
    GAME_UE4_26 = GameUtils.GameUe4Base + (26 << 16),
        GAME_GTATheTrilogyDefinitiveEdition = GAME_UE4_26 + 1,
        GAME_ReadyOrNot = GAME_UE4_26 + 2,
        GAME_BladeAndSoul = GAME_UE4_26 + 3,
        GAME_TowerOfFantasy = GAME_UE4_26 + 4,
        GAME_FinalFantasy7Rebirth = GAME_UE4_26 + 5,
        GAME_TheDivisionResurgence = GAME_UE4_26 + 6,
        GAME_StarWarsJediSurvivor = GAME_UE4_26 + 7,
        GAME_Snowbreak = GAME_UE4_26 + 8,
        GAME_TorchlightInfinite = GAME_UE4_26 + 9,
        GAME_QQ = GAME_UE4_26 + 10,
        GAME_WutheringWaves = GAME_UE4_26 + 11,
        GAME_DreamStar = GAME_UE4_26 + 12,
        GAME_MidnightSuns = GAME_UE4_26 + 13,
        GAME_FragPunk = GAME_UE4_26 + 14,
        GAME_RacingMaster = GAME_UE4_26 + 15,
        GAME_StellarBlade = GAME_UE4_26 + 16,
        GAME_EtheriaRestart = GAME_UE4_26 + 17,
    GAME_UE4_27 = GameUtils.GameUe4Base + (27 << 16),
        GAME_Splitgate = GAME_UE4_27 + 1,
        GAME_HYENAS = GAME_UE4_27 + 2,
        GAME_HogwartsLegacy = GAME_UE4_27 + 3,
        GAME_OutlastTrials = GAME_UE4_27 + 4,
        GAME_Valorant = GAME_UE4_27 + 5,
        GAME_Gollum = GAME_UE4_27 + 6,
        GAME_Grounded = GAME_UE4_27 + 7,
        GAME_DeltaForceHawkOps = GAME_UE4_27 + 8,
        GAME_MortalKombat1 = GAME_UE4_27 + 9,
        GAME_VisionsofMana = GAME_UE4_27 + 10,
        GAME_Spectre = GAME_UE4_27 + 11,
        GAME_KartRiderDrift = GAME_UE4_27 + 12,
        GAME_ThroneAndLiberty = GAME_UE4_27 + 13,
        GAME_MotoGP24 = GAME_UE4_27 + 14,
        GAME_Stray = GAME_UE4_27 + 15,
        GAME_CrystalOfAtlan = GAME_UE4_27 + 16,
        GAME_PromiseMascotAgency = GAME_UE4_27 + 17,
    GAME_UE4_28 = GameUtils.GameUe4Base + (28 << 16),

    GAME_UE4_LATEST = GAME_UE4_28,

    // TODO Figure out the enum name for UE5 Early Access
    // The commit https://github.com/EpicGames/UnrealEngine/commit/cf116088ae6b65c1701eee99288e43c7310d6bb1#diff-6178e9d97c98e321fc3f53770109ea7f6a8ea7a86cac542717a81922f2f93613R723
    // changed the IoStore and its packages format which breaks backward compatibility with 5.0.0-16433597+++UE5+Release-5.0-EarlyAccess
    GAME_UE5_0 = GameUtils.GameUe5Base + (0 << 16),
        GAME_MeetYourMaker = GAME_UE5_0 + 1,
        GAME_BlackMythWukong = GAME_UE5_0 + 2,
    GAME_UE5_1 = GameUtils.GameUe5Base + (1 << 16),
        GAME_3on3FreeStyleRebound = GAME_UE5_1 + 1,
        GAME_Stalker2 = GAME_UE5_1 + 2,
        GAME_TheCastingofFrankStone = GAME_UE5_1 + 3,
        GAME_SilentHill2Remake = GAME_UE5_1 + 4,
    GAME_UE5_2 = GameUtils.GameUe5Base + (2 << 16),
        GAME_DeadByDaylight = GAME_UE5_2 + 1,
        GAME_PaxDei = GAME_UE5_2 + 2,
        GAME_TheFirstDescendant = GAME_UE5_2 + 3,
        GAME_MetroAwakening = GAME_UE5_2 + 4,
        GAME_DuneAwakening = GAME_UE5_2 + 6,
    GAME_UE5_3 = GameUtils.GameUe5Base + (3 << 16),
        GAME_MarvelRivals = GAME_UE5_3 + 1,
        GAME_Placeholder = GAME_UE5_3 + 2, // Placeholder for a game that hasn't been added yet
        GAME_NobodyWantsToDie = GAME_UE5_3 + 3, // no use
        GAME_MonsterJamShowdown = GAME_UE5_3 + 4,
        GAME_Rennsport = GAME_UE5_3 + 5,
        GAME_AshesOfCreation = GAME_UE5_3 + 6,
        GAME_Avowed = GAME_UE5_3 + 7,
    GAME_UE5_4 = GameUtils.GameUe5Base + (4 << 16),
        GAME_FunkoFusion = GAME_UE5_4 + 1,
        GAME_InfinityNikki = GAME_UE5_4 + 2,
        GAME_NevernessToEverness = GAME_UE5_4 + 3,
        GAME_Gothic1Remake = GAME_UE5_4 + 4,
        GAME_SplitFiction = GAME_UE5_4 + 5,
        GAME_WildAssault = GAME_UE5_4 + 6,
        GAME_InZOI = GAME_UE5_4 + 7,
        GAME_TempestRising = GAME_UE5_4 + 8,
    GAME_UE5_5 = GameUtils.GameUe5Base + (5 << 16),
        GAME_Brickadia = GAME_UE5_5 + 1,
        GAME_Splitgate2 = GAME_UE5_5 + 2,
        GAME_DeadzoneRogue = GAME_UE5_5 + 3,
        GAME_MotoGP25 = GAME_UE5_5 + 4,
        GAME_Wildgate = GAME_UE5_5 + 5,
        GAME_ARKSurvivalAscended = GAME_UE5_5 + 6,
    GAME_UE5_6 = GameUtils.GameUe5Base + (6 << 16),
    GAME_UE5_7 = GameUtils.GameUe5Base + (7 << 16),

    GAME_UE5_LATEST = GAME_UE5_6
}

public static class GameUtils
{
    public const int GameUe4Base = 0x4000000;
    public const int GameUe5Base = 0x5000000;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GAME_UE4(int x)
    {
        return GameUe4Base + (x << 16);
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
                < EGame.GAME_UE5_1 => new FPackageFileVersion(522, 1004),
                < EGame.GAME_UE5_2 => new FPackageFileVersion(522, 1008),
                    EGame.GAME_TheFirstDescendant => new FPackageFileVersion(522, 1002),
                < EGame.GAME_UE5_4 => new FPackageFileVersion(522, 1009),
                < EGame.GAME_UE5_5 => new FPackageFileVersion(522, 1012),
                < EGame.GAME_UE5_6 => new FPackageFileVersion(522, 1013),
                _ => new FPackageFileVersion((int) EUnrealEngineObjectUE4Version.AUTOMATIC_VERSION, (int) EUnrealEngineObjectUE5Version.AUTOMATIC_VERSION)
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

public class EGameConverter : JsonConverter<EGame>
{
    public override void WriteJson(JsonWriter writer, EGame value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }

    public override EGame ReadJson(JsonReader reader, Type objectType, EGame existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer)
        {
            uint value = Convert.ToUInt32(reader.Value);
            return value > 0xFFFFFFF ? (EGame) ((value >> 28) + 3 << 24 | ((value >> 4) & 0xFF) << 16 | value & 0xF) : (EGame) value;
        }
        else if (reader is { TokenType: JsonToken.String, Value: string str })
        {
            return Enum.Parse<EGame>(str);
        }

        return EGame.GAME_UE4_LATEST;
    }
}
