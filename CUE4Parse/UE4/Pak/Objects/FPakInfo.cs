using System;
using System.Collections.Generic;
using System.IO;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.UE4.Pak.Objects
{
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


        PakFile_Version_Last,
        PakFile_Version_Invalid,
        PakFile_Version_Latest = PakFile_Version_Last - 1
    }

    public class FPakInfo
    {
        public const uint PAK_FILE_MAGIC = 0x5A6F12E1;
        public const uint PAK_FILE_MAGIC_OutlastTrials = 0xA590ED1E;
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

        private FPakInfo(FArchive Ar, OffsetsToTry offsetToTry)
        {
            var hottaVersion = 0u;
            if (Ar.Game == EGame.GAME_TowerOfFantasy && offsetToTry == OffsetsToTry.SizeHotta)
            {
                hottaVersion = Ar.Read<uint>();
                // Dirty way to keep backwards compatibility
                // This will work if the data at the end is compressed or encrypted which we don't know yet at this point
                if (hottaVersion > 255)
                {
                    hottaVersion = 0;
                }
            }

            // New FPakInfo fields.
            EncryptionKeyGuid = Ar.Read<FGuid>();          // PakFile_Version_EncryptionKeyGuid
            EncryptedIndex = Ar.Read<byte>() != 0;         // Do not replace by ReadFlag

            // Old FPakInfo fields
            Magic = Ar.Read<uint>();
            if (Magic != PAK_FILE_MAGIC)
            {
                if (Ar.Game == EGame.GAME_OutlastTrials && Magic == PAK_FILE_MAGIC_OutlastTrials) goto afterMagic;
                // Stop immediately when magic is wrong
                return;
            }

            afterMagic:
            Version = hottaVersion >= 2 ? (EPakFileVersion) (Ar.Read<int>() ^ 2) : Ar.Read<EPakFileVersion>();
            if (Ar.Game == EGame.GAME_StateOfDecay2)
            {
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                Version &= (EPakFileVersion) 0xFFFF;
            }

            IsSubVersion = Version == EPakFileVersion.PakFile_Version_FNameBasedCompressionMethod && offsetToTry == OffsetsToTry.Size8a;
            IndexOffset = Ar.Read<long>();
            if (Ar.Game == EGame.GAME_Snowbreak) IndexOffset ^= 0x1C1D1E1F;
            IndexSize = Ar.Read<long>();
            IndexHash = new FSHAHash(Ar);

            if (Ar.Game == EGame.GAME_MeetYourMaker && offsetToTry == OffsetsToTry.SizeHotta && Version >= EPakFileVersion.PakFile_Version_Latest)
            {
                var mymVersion = Ar.Read<uint>(); // I assume this is a version, only 0 right now.
            }

            if (Version == EPakFileVersion.PakFile_Version_FrozenIndex)
            {
                IndexIsFrozen = Ar.Read<byte>() != 0;
            }

            if (Version < EPakFileVersion.PakFile_Version_FNameBasedCompressionMethod)
            {
                CompressionMethods = new List<CompressionMethod>
                {
                    CompressionMethod.None, CompressionMethod.Zlib, CompressionMethod.Gzip, CompressionMethod.Oodle, CompressionMethod.LZ4, CompressionMethod.Zstd
                };
            }
            else
            {
                var maxNumCompressionMethods = offsetToTry switch
                {
                    OffsetsToTry.Size8a => 5,
                    OffsetsToTry.SizeHotta => 5,
                    OffsetsToTry.Size8 => 4,
                    OffsetsToTry.Size8_1 => 1,
                    OffsetsToTry.Size8_2 => 2,
                    OffsetsToTry.Size8_3 => 3,
                    _ => 4
                };

                unsafe
                {
                    var bufferSize = COMPRESSION_METHOD_NAME_LEN * maxNumCompressionMethods;
                    var buffer = stackalloc byte[bufferSize];
                    Ar.Serialize(buffer, bufferSize);
                    CompressionMethods = new List<CompressionMethod>(maxNumCompressionMethods + 1)
                    {
                        CompressionMethod.None
                    };
                    for (var i = 0; i < maxNumCompressionMethods; i++)
                    {
                        var name = new string((sbyte*) buffer + i * COMPRESSION_METHOD_NAME_LEN, 0, COMPRESSION_METHOD_NAME_LEN).TrimEnd('\0');
                        if (string.IsNullOrEmpty(name))
                            continue;
                        if (!Enum.TryParse(name, true, out CompressionMethod method))
                        {
                            Log.Warning($"Unknown compression method '{name}' in {Ar.Name}");
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

            // Reset new fields to their default states when seralizing older pak format.
            if (Version < EPakFileVersion.PakFile_Version_IndexEncryption)
            {
                EncryptedIndex = default;
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
            Size8_1 = Size + 32,
            Size8_2 = Size8_1 + 32,
            Size8_3 = Size8_2 + 32,
            Size8 = Size8_3 + 32, // added size of CompressionMethods as char[32]
            Size8a = Size8 + 32, // UE4.23 - also has version 8 (like 4.22) but different pak file structure
            Size9 = Size8a + 1, // UE4.25
            //Size10 = Size8a

            SizeHotta = Size8a + 4, // additional int for custom pak version

            SizeLast,
            SizeMax = SizeLast - 1
        }

        private static readonly OffsetsToTry[] _offsetsToTry =
        {
            OffsetsToTry.Size8a,
            OffsetsToTry.Size8,
            OffsetsToTry.Size,
            OffsetsToTry.Size9,

            OffsetsToTry.Size8_1,
            OffsetsToTry.Size8_2,
            OffsetsToTry.Size8_3
        };

        public static FPakInfo ReadFPakInfo(FArchive Ar)
        {
            unsafe
            {
                var length = Ar.Length;
                const long maxOffset = (long) OffsetsToTry.SizeMax;
                if (length < maxOffset)
                {
                    throw new ParserException($"File {Ar.Name} is too small to be a pak file");
                }
                Ar.Seek(-maxOffset, SeekOrigin.End);
                var buffer = stackalloc byte[(int) maxOffset];
                Ar.Serialize(buffer, (int) maxOffset);

                var reader = new FPointerArchive(Ar.Name, buffer, maxOffset, Ar.Versions);

                var offsetsToTry = Ar.Game switch
                {
                    EGame.GAME_TowerOfFantasy or EGame.GAME_MeetYourMaker => new [] { OffsetsToTry.SizeHotta },
                    _ => _offsetsToTry
                };
                foreach (var offset in offsetsToTry)
                {
                    reader.Seek(-(long) offset, SeekOrigin.End);
                    var info = new FPakInfo(reader, offset);

                    if (Ar.Game == EGame.GAME_OutlastTrials && info.Magic == PAK_FILE_MAGIC_OutlastTrials) return info;
                    if (info.Magic == PAK_FILE_MAGIC)
                    {
                        return info;
                    }
                }
            }
            throw new ParserException($"File {Ar.Name} has an unknown format");
        }
    }
}
