using System;
using System.Collections.Generic;
using System.IO;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Serilog;

namespace CUE4Parse.UE4.Pak.Objects
{
    public enum EPakFileVersion : int
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
        public readonly FGuid EncryptionKeyGuid;
        public readonly List<CompressionMethod> CompressionMethods;

        private FPakInfo(FArchive Ar, OffsetsToTry offsetToTry)
        {
            // New FPakInfo fields.
            EncryptionKeyGuid = Ar.Read<FGuid>();          // PakFile_Version_EncryptionKeyGuid
            EncryptedIndex = Ar.ReadFlag();                // PakFile_Version_IndexEncryption
            
            // Old FPakInfo fields
            Magic = Ar.Read<uint>();
            if (Magic != PAK_FILE_MAGIC)
            {
                // Stop immediately when magic is wrong
                return;
            }

            Version = Ar.Read<EPakFileVersion>();
            IsSubVersion = (Version == EPakFileVersion.PakFile_Version_FNameBasedCompressionMethod && offsetToTry == OffsetsToTry.Size8a);
            IndexOffset = Ar.Read<long>();
            IndexSize = Ar.Read<long>();
            IndexHash = new FSHAHash(Ar);

            if (Version == EPakFileVersion.PakFile_Version_FrozenIndex)
            {
                var bIndexIsFrozen = Ar.ReadFlag();
                // used just for 4.25, so don't do any support unless it's really needed
                if (bIndexIsFrozen)
                    throw new ParserException(Ar, "Pak index is frozen");
            }

            if (Version < EPakFileVersion.PakFile_Version_FNameBasedCompressionMethod)
            {
                CompressionMethods = new List<CompressionMethod>
                {
                    CompressionMethod.None, CompressionMethod.Zlib, CompressionMethod.Gzip, CompressionMethod.Custom, CompressionMethod.Oodle
                };
            }
            else
            {
                var maxNumCompressionMethods = offsetToTry switch
                {
                    OffsetsToTry.Size8a => 5,
                    OffsetsToTry.Size8 => 4,
                    OffsetsToTry.Size8_1 => 1,
                    OffsetsToTry.Size8_2 => 2,
                    OffsetsToTry.Size8_3 => 3,
                    _ => 0
                };

                unsafe
                {
                    var bufferSize = COMPRESSION_METHOD_NAME_LEN * maxNumCompressionMethods;
                    var buffer = stackalloc byte[bufferSize];
                    Ar.Read(buffer, bufferSize);
                    CompressionMethods = new List<CompressionMethod>(maxNumCompressionMethods + 1)
                    {
                        CompressionMethod.None
                    };
                    for (var i = 0; i < maxNumCompressionMethods; i++)
                    {
                        var name = new string((sbyte*) buffer + i * COMPRESSION_METHOD_NAME_LEN, 0, COMPRESSION_METHOD_NAME_LEN).TrimEnd('\0');
                        if (string.IsNullOrEmpty(name))
                            continue;
                        if (!Enum.TryParse(name, out CompressionMethod method))
                        {
                            Log.Warning($"Unknown compression method '{name}' in {Ar.Name}");
                            method = CompressionMethod.Unknown;
                        }
                        CompressionMethods.Add(method);
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
            
            SizeLast,
            SizeMax = SizeLast - 1
        }

        private static OffsetsToTry[] _offsetsToTry =
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
                Ar.Read(buffer, (int) maxOffset);
                
                var reader = new FPointerArchive(Ar.Name, buffer, (long) OffsetsToTry.SizeMax, Ar.Game, Ar.Ver);

                foreach (var offset in _offsetsToTry)
                {
                    reader.Seek(-(long)offset, SeekOrigin.End);
                    var info = new FPakInfo(reader, offset);
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