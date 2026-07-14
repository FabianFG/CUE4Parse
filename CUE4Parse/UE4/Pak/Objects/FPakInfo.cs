using System.Buffers.Binary;
using CUE4Parse.Compression;
using CUE4Parse.GameTypes.ABI.Encryption.SM4;
using CUE4Parse.GameTypes.Tencent.ValorantSource.Encryption;
using CUE4Parse.GameTypes.Tencent.ValorantSource.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Serilog;

namespace CUE4Parse.UE4.Pak.Objects;

public enum EPakFileVersion
{
    PakFile_Version_Initial = 1,
    PakFile_Version_NoTimestamps = 2,
    PakFile_Version_CompressionEncryption = 3,
    PakFile_Version_IndexEncryption = 4,
    PakFile_Version_RelativeChunkOffsets = 5,
    PakFile_Version_DeleteRecords = 6,
    PakFile_Version_EncryptionKeyGuid = 7,
    PakFile_Version_FNameBasedCompressionMethod = 8,
    PakFile_Version_FrozenIndex = 9,
    PakFile_Version_PathHashIndex = 10,
    PakFile_Version_Fnv64BugFix = 11,
    PakFile_Version_Utf8PakDirectory = 12,
    PakFile_Version_SortedDirectoryIndex = 13, // FullDirectoryIndex stored as a flat FPakFlatDirectoryIndex.
    PakFile_Version_PakchunkIndex = 14, // PakchunkIndex stored in the trailer so it doesn't have to be derived from the filename.

    PakFile_Version_Last,
    PakFile_Version_Invalid,
    PakFile_Version_Latest = PakFile_Version_Last - 1
}

public partial class FPakInfo
{
    public const uint PAK_FILE_MAGIC = 0x5A6F12E1;
    public const uint PAK_FILE_MAGIC_OutlastTrials = 0xA590ED1E;
    public const uint PAK_FILE_MAGIC_TorchlightInfinite = 0x6B2A56B8;
    public const uint PAK_FILE_MAGIC_WildAssault = 0xA4CCD123;
    public const uint PAK_FILE_MAGIC_Gameloop_Undawn = 0x5A6F12EC;
    public const uint PAK_FILE_MAGIC_FridayThe13th = 0x65617441;
    public const uint PAK_FILE_MAGIC_DreamStar = 0x1B6A32F1;
    public const uint PAK_FILE_MAGIC_GameForPeace = 0xff67ff70;
    public const uint PAK_FILE_MAGIC_KartRiderDrift = 0x81c4b35b;
    public const uint PAK_FILE_MAGIC_RacingMaster = 0x9a51da3f;
    public const uint PAK_FILE_MAGIC_CrystalOfAtlan = 0x22ce976a;
    public const uint PAK_FILE_MAGIC_PromiseMascotAgency = 0x11adde11;
    public const uint PAK_FILE_MAGIC_ArenaBreakoutInfinite = 0x53647586;
    public const uint PAK_FILE_MAGIC_ArenaBreakoutMobile = 0x57647587;
    public const uint PAK_FILE_MAGIC_AssaultFireFuture = 0x4F6FAE86;
    public const uint PAK_FILE_MAGIC_Back4Blood = 0x18772;
    public const uint PAK_FILE_MAGIC_SilverPalace = 0x12E15A6F;
    public const uint PAK_FILE_MAGIC_ValorantSource = 0x167C2AB4;

    public const int COMPRESSION_METHOD_NAME_LEN = 32;

    public readonly uint Magic;
    public readonly EPakFileVersion Version;
    public readonly bool IsSubVersion;
    public readonly long IndexOffset;
    public readonly long IndexSize;
    public readonly FSHAHash IndexHash;
    // When new fields are added to FPakInfo, they're serialized before 'Magic' to keep compatibility
    // with older pak file versions. At the same time, structure size grows.
    public readonly bool EncryptedIndex;
    public readonly bool IndexIsFrozen;
    public readonly FGuid EncryptionKeyGuid;
    public readonly List<CompressionMethod> CompressionMethods;
    public readonly int PakchunkIndex = -1; // INDEX_NONE
    public byte[] CustomEncryptionData { get; private set; }

    private FPakInfo(FArchive Ar, OffsetsToTry offsetToTry)
    {
        var startPosition = Ar.Position;

        var hottaVersion = 0u;
        if (Ar.Game == GAME_TowerOfFantasy && offsetToTry == OffsetsToTry.SizeHotta)
        {
            hottaVersion = Ar.Read<uint>();
            // Dirty way to keep backwards compatibility
            // This will work if the data at the end is compressed or encrypted which we don't know yet at this point
            if (hottaVersion > 255)
            {
                hottaVersion = 0;
            }
        }

        if (Ar.Game is GAME_TorchlightInfinite or GAME_EtheriaRestart) Ar.Position += 3;

        if (Ar.Game == GAME_GameForPeace)
        {
            EncryptionKeyGuid = default;
            EncryptedIndex = Ar.Read<byte>() != 0x6c;
            Magic = Ar.Read<uint>();
            if (Magic != PAK_FILE_MAGIC_GameForPeace) return;
            Version = Ar.Read<EPakFileVersion>();
            if (Version >= EPakFileVersion.PakFile_Version_PathHashIndex)
            {
                Version = EPakFileVersion.PakFile_Version_FNameBasedCompressionMethod;// Override to force readIndexLegacy
            }
            IndexHash = new FSHAHash(Ar);
            IndexSize = (long)(Ar.Read<ulong>() ^ 0x8924b0e3298b7069);
            IndexOffset = (long) (Ar.Read<ulong>() ^ 0xd74af37faa6b020d);
            CompressionMethods =
            [
                CompressionMethod.None, CompressionMethod.Zlib, CompressionMethod.Gzip, CompressionMethod.Oodle,
                CompressionMethod.LZ4, CompressionMethod.Zstd
            ];
            return;
        }

        if (Ar.Game is GAME_ArenaBreakoutMobile)
        {
            Magic = Ar.Read<uint>();
            // Global or maybe older versions
            if (Magic == PAK_FILE_MAGIC_ArenaBreakoutInfinite)
            {
                EncryptionKeyGuid = default;
                EncryptedIndex = Ar.Read<byte>() != 0;
                IndexSize = Ar.Read<long>();
                IndexOffset = Ar.Read<long>();
                IndexHash = new FSHAHash(Ar);
                Version = Ar.Read<EPakFileVersion>();
                goto beforeCompression;
            }

            // Chinese mobile version
            if (Magic == PAK_FILE_MAGIC_ArenaBreakoutMobile)
            {
                EncryptionKeyGuid = default;
                EncryptedIndex = Ar.Read<byte>() != 0;
                var encryptedIndexInfo = Ar.ReadBytes(16);
                var indexInfo = new byte[16];
                Buffer.BlockCopy(encryptedIndexInfo, 8, indexInfo, 0, 8);
                Buffer.BlockCopy(encryptedIndexInfo, 0, indexInfo, 8, 8);
                ABIDecryption.DecryptAbiMobilePakInfo(indexInfo);
                IndexOffset = BinaryPrimitives.ReadInt64LittleEndian(indexInfo);
                IndexSize = BinaryPrimitives.ReadInt64LittleEndian(indexInfo.AsSpan(8));
                IndexHash = new FSHAHash(Ar);
                Version = Ar.Read<EPakFileVersion>();
                goto beforeCompression;
            }
        }

        if (Ar.Game == GAME_ArenaBreakoutInfinite)
        {
            EncryptionKeyGuid = Ar.Read<FGuid>();
            Magic = Ar.Read<uint>();
            if (Magic != PAK_FILE_MAGIC_ArenaBreakoutInfinite) return;
            EncryptedIndex = Ar.Read<byte>() != 0;
            IndexSize = Ar.Read<long>();
            IndexOffset = Ar.Read<long>();
            IndexHash = new FSHAHash(Ar);
            Version = Ar.Read<EPakFileVersion>();
            goto beforeCompression;
        }

        if (Ar.Game == GAME_DragonQuestXI)
        {
            EncryptionKeyGuid = default;
            EncryptedIndex = Ar.Read<byte>() != 0;
            Magic = Ar.Read<uint>();
            if (Magic != PAK_FILE_MAGIC) return;
            Version = Ar.Read<EPakFileVersion>();
            IndexOffset = Ar.Read<long>();
            IndexSize = Ar.Read<long>();
            IndexHash = new FSHAHash(Ar);
            goto beforeCompression;
        }

        if (Ar.Game == GAME_RacingMaster)
        {
            EncryptedIndex = Ar.ReadFlag();
            EncryptionKeyGuid = Ar.Read<FGuid>();
            CustomEncryptionData = Ar.ReadBytes(4);
            Magic = Ar.Read<uint>();
            if (Magic != PAK_FILE_MAGIC_RacingMaster) return;
            IndexSize = Ar.Read<long>();
            IndexHash = new FSHAHash(Ar);
            Version = Ar.Read<EPakFileVersion>();
            IndexOffset = Ar.Read<long>();
            goto beforeCompression;
        }

        if (Ar.Game == GAME_PromiseMascotAgency)
        {
            EncryptionKeyGuid = Ar.Read<FGuid>();
            EncryptedIndex = Ar.ReadFlag();
            Magic = Ar.Read<uint>();
            if (Magic != PAK_FILE_MAGIC_PromiseMascotAgency)
                return;
            IndexHash = new FSHAHash(Ar);
            Version = (EPakFileVersion)(11 + (Ar.Read<int>() ^ 0x0A4FFC11));
            Ar.Position += 8;
            IndexSize = Ar.Read<long>() ^ 0x0BBEFB6F91D3B57B;
            IndexOffset = Ar.Read<long>();
            goto beforeCompression;
        }

        if (Ar.Game == GAME_CrystalOfAtlan)
        {
            EncryptedIndex = Ar.ReadFlag();
            Version = Ar.Read<EPakFileVersion>();
            IndexSize = Ar.Read<long>();
            IndexHash = new FSHAHash(Ar);
            IndexOffset = Ar.Read<long>();
            Magic = Ar.Read<uint>();
            if (Magic != PAK_FILE_MAGIC_CrystalOfAtlan)
                return;
            EncryptionKeyGuid = Ar.Read<FGuid>();
            goto beforeCompression;
        }

        if (Ar.Game == GAME_DuneAwakening)
        {
            var magic = Ar.Read<uint>();
            if (magic != 0xA590ED1E) return;
            IndexOffset = Ar.Read<long>();
            IndexSize = Ar.Read<long>();
            IndexHash = new FSHAHash(Ar);
            EncryptionKeyGuid = Ar.Read<FGuid>();
            EncryptedIndex = Ar.ReadFlag();
            Magic = Ar.Read<uint>();
            if (Magic != PAK_FILE_MAGIC) return;
            Version = Ar.Read<EPakFileVersion>();
            Ar.Position += 36; // another index size/offset/hash
            goto beforeCompression;
        }

        if (Ar.Game is GAME_Back4Blood) // Reversed by Spiritovod
        {
            Version = Ar.Read<EPakFileVersion>();
            Magic = Ar.Read<uint>();
            if (Magic != PAK_FILE_MAGIC_Back4Blood) return;
            EncryptedIndex = Ar.Read<byte>() != 0;
            EncryptionKeyGuid = Ar.Read<FGuid>();
            IndexOffset = Ar.Read<long>();
            IndexSize = Ar.Read<long>();
            IndexHash = new FSHAHash(Ar);

            if (IndexSize > Ar.Length || IndexSize < 0)
            {
                Ar.Position = startPosition + 4;
                Magic = Ar.Read<uint>();
                if (Magic != PAK_FILE_MAGIC_Back4Blood) return;
                EncryptionKeyGuid = default;
                Ar.Position += 16;
                EncryptedIndex = Ar.Read<byte>() != 0;
                IndexHash = new FSHAHash(Ar);
                IndexSize = Ar.Read<long>();
                IndexOffset = Ar.Read<long>();
            }

            if (Ar.Position < Ar.Length)
            {
                var check = Ar.Read<byte>();
                if (check > 1)
                {
                    Ar.Position--;
                }
            }

            goto beforeCompression;
        }

        if (Ar.Game is GAME_ValorantSource)
        {
            Magic = Ar.Read<uint>();
            if (Magic is not PAK_FILE_MAGIC_ValorantSource) return;
            Version = Ar.Read<EPakFileVersion>();
            EncryptionKeyGuid = Ar.Read<FGuid>();
            Ar.Read<byte>(); // Tencent uses this byte as the cipher selector
            EncryptedIndex = Ar.Read<byte>() != 0;

            var valorantRsaKeyOffset = Ar.Read<long>();
            Ar.Position += EncryptionKeyGuid.A % 5 + 1;
            Ar.Read<long>();
            var maskedIndexOffset = Ar.Read<ulong>();
            Ar.Position += EncryptionKeyGuid.B % 5 + 1;
            var valorantRsaKeySize = checked((int) Ar.Read<long>());
            Ar.Read<long>();
            Ar.Position += EncryptionKeyGuid.C % 5 + 1;
            Ar.Read<long>();
            var maskedIndexSize = Ar.Read<ulong>();

            CustomEncryptionData = new byte[sizeof(long) + sizeof(int)];
            BinaryPrimitives.WriteInt64LittleEndian(CustomEncryptionData.AsSpan(0, sizeof(long)), valorantRsaKeyOffset);
            BinaryPrimitives.WriteInt32LittleEndian(CustomEncryptionData.AsSpan(sizeof(long), sizeof(int)), valorantRsaKeySize);

            const ulong offsetMask = ValorantSourceAes.LOW_NIBBLES_MASK;
            const ulong sizeMask = ValorantSourceAes.HIGH_NIBBLES_MASK;
            IndexOffset = (long) ((maskedIndexSize & offsetMask) | (maskedIndexOffset & ~offsetMask));
            IndexSize = (long) ((maskedIndexSize & sizeMask) | (maskedIndexOffset & ~sizeMask));
            IndexHash = new FSHAHash(Ar);

            // I'm not reading footer exactly rigth so hardcoded offset for compression names
            Ar.Position = 126;
            goto beforeCompression;
        }

        // New FPakInfo fields.
        EncryptionKeyGuid = Ar.Read<FGuid>();          // PakFile_Version_EncryptionKeyGuid
        EncryptedIndex = Ar.Read<byte>() != 0;         // Do not replace by ReadFlag

        // Old FPakInfo fields
        Magic = Ar.Read<uint>();
        if (Magic != PAK_FILE_MAGIC)
        {
            if (Ar.Game == GAME_OutlastTrials && Magic == PAK_FILE_MAGIC_OutlastTrials ||
                Ar.Game is GAME_TorchlightInfinite or GAME_EtheriaRestart && Magic == PAK_FILE_MAGIC_TorchlightInfinite ||
                Ar.Game == GAME_WildAssault && Magic == PAK_FILE_MAGIC_WildAssault ||
                Ar.Game == GAME_Undawn && Magic == PAK_FILE_MAGIC_Gameloop_Undawn ||
                Ar.Game == GAME_FridayThe13th && Magic == PAK_FILE_MAGIC_FridayThe13th ||
                Ar.Game == GAME_DreamStar && Magic == PAK_FILE_MAGIC_DreamStar ||
                Ar.Game == GAME_AssaultFireFuture && Magic == PAK_FILE_MAGIC_AssaultFireFuture ||
                Ar.Game == GAME_KartRiderDrift && Magic == PAK_FILE_MAGIC_KartRiderDrift ||
                Ar.Game == GAME_SilverPalace && Magic == PAK_FILE_MAGIC_SilverPalace)
                goto afterMagic;
            // Stop immediately when magic is wrong
            return;
        }

        afterMagic:
        Version = hottaVersion >= 2 ? (EPakFileVersion) (Ar.Read<int>() ^ 2) : Ar.Read<EPakFileVersion>();
        if (Ar.Game == GAME_LordOfMysteries && ((uint) Version & 0x80000000) != 0)
        {
            Version = (EPakFileVersion) ((uint) Version & 0x7FFFFFFF);
            IndexHash = new FSHAHash(Ar);
            IndexOffset = Ar.Read<long>();
            IndexSize = Ar.Read<long>() >> 1;
            goto beforeCompression;
        }
        
        if (Ar.Game == GAME_StateOfDecay2)
        {
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            Version &= (EPakFileVersion) 0xFFFF;
        }

        if (Ar.Game == GAME_KartRiderDrift)
        {
            Version &= (EPakFileVersion) 0x0F;
        }

        if (Ar.Game == GAME_FridayThe13th)
        {
            if (!EncryptedIndex && Magic == 0 && (int) Version == PAK_FILE_MAGIC)
            {
                Magic = PAK_FILE_MAGIC;
                Version = Ar.Read<EPakFileVersion>();
            }

            if (Version >= EPakFileVersion.PakFile_Version_RelativeChunkOffsets) // PakFile_Version_IllFonic
            {
                Version = EPakFileVersion.PakFile_Version_IndexEncryption; // Actual version
                Ar.Position += 4; // ExtraMagic
            }
        }

        IsSubVersion = Version == EPakFileVersion.PakFile_Version_FNameBasedCompressionMethod && offsetToTry == OffsetsToTry.Size8a;
        if (Ar.Game is GAME_TorchlightInfinite or GAME_EtheriaRestart) Ar.Position += 1;
        if (Ar.Game == GAME_BlackMythWukong) Ar.Position += 2;
        IndexOffset = Ar.Read<long>();
        if (Ar.Game == GAME_Farlight84) Ar.Position += 8; // unknown long
        if (Ar.Game == GAME_Snowbreak) IndexOffset ^= 0x1C1D1E1F;
        if (Ar.Game == GAME_KartRiderDrift) IndexOffset ^= 0x3009EB;
        if (Ar.Game is GAME_NevernessToEverness or GAME_NevernessToEverness_CBT2) IndexOffset -= 1;
        IndexSize = Ar.Read<long>();
        IndexHash = new FSHAHash(Ar);

        if (Ar.Game is GAME_DreamStar or GAME_AssaultFireFuture)
        {
            (IndexOffset, IndexSize) = (IndexSize, IndexOffset);
        }

        if (Ar.Game == GAME_MeetYourMaker && offsetToTry == OffsetsToTry.SizeHotta && Version >= EPakFileVersion.PakFile_Version_Fnv64BugFix)
        {
            var mymVersion = Ar.Read<uint>(); // I assume this is a version, only 0 right now.
        }

        if (Ar.Game == GAME_WildAssault)
        {
            EncryptionKeyGuid = default;
            IndexOffset = (long) ((ulong) IndexOffset ^ 0xD5B9B05CE8143A3C) - 0xAA;
            IndexSize = (long) ((ulong) IndexSize ^ 0x6DB425B4BC084B4B) - 0xA8;
        }

        if (Ar.Game is GAME_SilverPalace)
        {
            IndexOffset = (long) ((ulong) IndexOffset ^ 0x8b3c9f2a5e1d7046);
            IndexSize = (long) ((ulong) IndexSize ^ 0x8b3c9f2a5e1d7046);
        }

        if (Ar.Game is GAME_DeadByDaylight or GAME_DeadByDaylight_Old)
        {
            CustomEncryptionData = Ar.ReadBytes(28);
            _ = Ar.Read<uint>();
        }

        if (Ar.Game == GAME_OnePieceAmbition)
        {
            var currentPosition = Ar.Position;
            Ar.Position = IndexOffset;
            var shift = Ar.Read<long>();
            IndexOffset = Ar.Read<long>();
            shift = ~shift;
            IndexOffset ^= shift;
            IndexSize = startPosition - IndexOffset - 17;
            Ar.Position = currentPosition;
        }

        if (Version == EPakFileVersion.PakFile_Version_FrozenIndex)
        {
            IndexIsFrozen = Ar.Read<byte>() != 0;
        }

        beforeCompression:
        if (Version < EPakFileVersion.PakFile_Version_FNameBasedCompressionMethod)
        {
            CompressionMethods =
            [
                CompressionMethod.None, CompressionMethod.Zlib, CompressionMethod.Gzip, CompressionMethod.Oodle, CompressionMethod.LZ4, CompressionMethod.Zstd
            ];
        }
        else
        {
            var maxNumCompressionMethods = offsetToTry switch
            {
                OffsetsToTry.Size8a => 5,
                OffsetsToTry.SizeHotta => 5,
                OffsetsToTry.SizeDbD => 5,
                OffsetsToTry.SizeRennsport => 5,
                OffsetsToTry.SizeBack4Blood => 5,
                OffsetsToTry.SizeArenaBreakoutMobile => 5,
                OffsetsToTry.SizeValorantSource => 5,
                OffsetsToTry.Size8 => 4,
                OffsetsToTry.Size8_1 => 1,
                OffsetsToTry.Size8_2 => 2,
                OffsetsToTry.Size8_3 => 3,
                _ => 4
            };

            unsafe
            {
                var length = Ar.Game == GAME_KartRiderDrift ? 48 : COMPRESSION_METHOD_NAME_LEN;
                var bufferSize = length * maxNumCompressionMethods;
                var buffer = stackalloc byte[bufferSize];
                Ar.Serialize(buffer, bufferSize);
                CompressionMethods = new List<CompressionMethod>(maxNumCompressionMethods + 1)
                {
                    CompressionMethod.None
                };
                for (var i = 0; i < maxNumCompressionMethods; i++)
                {
                    var name = new string((sbyte*) buffer + i * length, 0, length).TrimEnd('\0');
                    if (string.IsNullOrEmpty(name))
                        continue;
                    if (!Enum.TryParse(name, true, out CompressionMethod method))
                    {
                        Log.Warning("Unknown compression method '{CompressionMethod}' in {ArchiveName}", name, Ar.Name);
                        method = CompressionMethod.Unknown;
                    }
                    CompressionMethods.Add(method);
                }
                if (hottaVersion >= 3)
                {
                    CompressionMethods.Remove(0);
                }
            }
        }

        // Written at the tail so the trailer for older versions remains byte-compatible. Paks authored before
        // this version leave PakchunkIndex at INDEX_NONE, and the reader falls back to deriving it from the filename.
        if (Version >= EPakFileVersion.PakFile_Version_PakchunkIndex && Ar.Game >= GAME_UE5_9)
        {
            PakchunkIndex = Ar.Read<int>();
        }

        // Reset new fields to their default states when seralizing older pak format.
        if (Version < EPakFileVersion.PakFile_Version_IndexEncryption)
        {
            EncryptedIndex = false;
        }

        if (Version < EPakFileVersion.PakFile_Version_EncryptionKeyGuid)
        {
            EncryptionKeyGuid = default;
        }
    }

    private enum OffsetsToTry
    {
        Size = sizeof(int) * 2 + sizeof(long) * 2 + 20 + /* new fields */ 1 + 16, // sizeof(FGuid)
        // Just to be sure
        SizeGameForPeace = 45,
        Size8_1 = Size + 32,
        Size8_2 = Size8_1 + 32,
        Size8_3 = Size8_2 + 32,
        Size8 = Size8_3 + 32, // added size of CompressionMethods as char[32]
        Size8a = Size8 + 32, // UE4.23 - also has version 8 (like 4.22) but different pak file structure
        Size9 = Size8a + 1, // UE4.25
        Size9a = Size9 + 4, // UE6.0 - Added pakchunk index int32
        SizeB1 = Size9 + 1, // plus 1
        //Size10 = Size8a

        SizeRacingMaster = Size8 + 4, // additional int
        SizeFTT = Size + 4, // additional int for extra magic
        SizeHotta = Size8a + 4, // additional int for custom pak version
        SizeARKSurvivalAscended = Size8a + 8, // additional 8 bytes
        SizeFarlight = Size8a + 9, // additional long and byte
        SizeDreamStar = Size8a + 10,
        SizeRennsport = Size8a + 16,
        SizeQQ = Size8a + 26,
        SizeDbD = Size8a + 32, // additional 28 bytes for encryption key and 4 bytes for unknown uint

        SizeLast,
        SizeMax = SizeLast - 1,
        SizeBack4Blood = 222,
        SizeArenaBreakoutMobile = 205,
        SizeDuneAwakening = 261,
        SizeValorantSource = 286, // For older versions it was 282
        SizeKartRiderDrift = 397, // don't let this be SizeMax, it's way above average and cause issues
    }

    private static readonly OffsetsToTry[] _offsetsToTry =
    [
        OffsetsToTry.Size8a,
        OffsetsToTry.Size8,
        OffsetsToTry.Size,
        OffsetsToTry.Size9,
        OffsetsToTry.Size9a,

        OffsetsToTry.Size8_1,
        OffsetsToTry.Size8_2,
        OffsetsToTry.Size8_3
    ];

    public static FPakInfo ReadFPakInfo(FArchive Ar)
    {
        unsafe
        {
            var length = Ar.Length;
            var maxOffset = Ar.Game switch
            {
                GAME_Back4Blood => (long) OffsetsToTry.SizeBack4Blood,
                GAME_DuneAwakening => (long) OffsetsToTry.SizeDuneAwakening,
                GAME_KartRiderDrift => (long) OffsetsToTry.SizeKartRiderDrift,
                GAME_ArenaBreakoutMobile => (long) OffsetsToTry.SizeArenaBreakoutMobile,
                GAME_ValorantSource => (long) OffsetsToTry.SizeValorantSource,
                _ => Math.Min(length, (long) OffsetsToTry.SizeMax),
            };

            Ar.Seek(-maxOffset, SeekOrigin.End);
            var buffer = stackalloc byte[(int) maxOffset];
            Ar.Serialize(buffer, (int) maxOffset);

            switch (Ar.Game)
            {
                case GAME_InZOI:
                    DecryptInZOIFPakInfo(Ar, maxOffset, buffer);
                    break;
                case GAME_ValorantSource:
                    DecryptValorantSourcePakInfo(Ar, maxOffset, buffer);
                    break;
            }

            using var reader = new FPointerArchive(Ar.Name, buffer, maxOffset, Ar.Versions);

            var offsetsToTry = Ar.Game switch
            {
                GAME_TowerOfFantasy or GAME_MeetYourMaker or GAME_TorchlightInfinite or GAME_EtheriaRestart => [OffsetsToTry.SizeHotta],
                GAME_FridayThe13th => [OffsetsToTry.SizeFTT],
                GAME_DeadByDaylight or GAME_DeadByDaylight_Old => [OffsetsToTry.SizeDbD],
                GAME_Farlight84 => [OffsetsToTry.SizeFarlight],
                GAME_QQ or GAME_DreamStar => [OffsetsToTry.SizeDreamStar, OffsetsToTry.SizeQQ],
                GAME_GameForPeace or GAME_DragonQuestXI => [OffsetsToTry.SizeGameForPeace],
                GAME_BlackMythWukong => [OffsetsToTry.SizeB1],
                GAME_Rennsport => [OffsetsToTry.SizeRennsport],
                GAME_RacingMaster => [OffsetsToTry.SizeRacingMaster],
                GAME_ARKSurvivalAscended or GAME_PromiseMascotAgency => [OffsetsToTry.SizeARKSurvivalAscended],
                GAME_KartRiderDrift => [.._offsetsToTry, OffsetsToTry.SizeKartRiderDrift],
                GAME_DuneAwakening => [OffsetsToTry.SizeDuneAwakening],
                GAME_Back4Blood => [OffsetsToTry.SizeBack4Blood],
                GAME_ArenaBreakoutMobile => [OffsetsToTry.SizeArenaBreakoutMobile, OffsetsToTry.Size8a],
                GAME_ValorantSource => [OffsetsToTry.SizeValorantSource],
                _ => _offsetsToTry
            };

            foreach (var offset in offsetsToTry)
            {
                if ((long) offset > maxOffset) continue;

                reader.Seek(-(long) offset, SeekOrigin.End);
                FPakInfo info;
                if (Ar.Game == GAME_OnePieceAmbition)
                {
                    var currentOffset = Ar.Position;
                    Ar.Position -= (long)offset;
                    info = new FPakInfo(Ar, offset);
                    Ar.Position = currentOffset;
                }
                else
                {
                    info = new FPakInfo(reader, offset);
                }

                var found = Ar.Game switch
                {
                    GAME_FridayThe13th when info.Magic == PAK_FILE_MAGIC_FridayThe13th => true,
                    GAME_GameForPeace when info.Magic == PAK_FILE_MAGIC_GameForPeace => true,
                    GAME_Undawn when info.Magic == PAK_FILE_MAGIC_Gameloop_Undawn => true,
                    GAME_TorchlightInfinite or GAME_EtheriaRestart when info.Magic == PAK_FILE_MAGIC_TorchlightInfinite => true,
                    GAME_DreamStar when info.Magic == PAK_FILE_MAGIC_DreamStar => true,
                    GAME_RacingMaster when info.Magic == PAK_FILE_MAGIC_RacingMaster => true,
                    GAME_OutlastTrials when info.Magic == PAK_FILE_MAGIC_OutlastTrials => true,
                    GAME_KartRiderDrift when info.Magic == PAK_FILE_MAGIC_KartRiderDrift => true,
                    GAME_CrystalOfAtlan when info.Magic == PAK_FILE_MAGIC_CrystalOfAtlan => true,
                    GAME_PromiseMascotAgency when info.Magic == PAK_FILE_MAGIC_PromiseMascotAgency => true,
                    GAME_WildAssault when info.Magic == PAK_FILE_MAGIC_WildAssault => true,
                    GAME_ArenaBreakoutInfinite when info.Magic == PAK_FILE_MAGIC_ArenaBreakoutInfinite => true,
                    GAME_ArenaBreakoutMobile when info.Magic is PAK_FILE_MAGIC_ArenaBreakoutInfinite or PAK_FILE_MAGIC_ArenaBreakoutMobile => true,
                    GAME_AssaultFireFuture when info.Magic == PAK_FILE_MAGIC_AssaultFireFuture => true,
                    GAME_Back4Blood when info.Magic == PAK_FILE_MAGIC_Back4Blood => true,
                    GAME_SilverPalace when info.Magic == PAK_FILE_MAGIC_SilverPalace => true,
                    GAME_ValorantSource when info.Magic == PAK_FILE_MAGIC_ValorantSource => true,
                    _ => info.Magic == PAK_FILE_MAGIC
                };
                if (found)
                {
                    if (Ar.Game is GAME_ValorantSource)
                    {
                        info.CustomEncryptionData = ValorantSourceRSA.DerivePakKey(Ar, info.CustomEncryptionData);
                    }

                    return info;
                }
            }
        }
        throw new ParserException($"File {Ar.Name} has an unknown format");
    }
}
