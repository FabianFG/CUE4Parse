using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak.Reader;
using System.Runtime.InteropServices;
using System.Text;

namespace CUE4Parse.UE4.Objects.PakFile
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
	};

	public readonly struct FPakInfo
    {
        /** Magic number to use in header */
        private const uint _PAKFILE_MAGIC = 0x5A6F12E1;
        /** Size of cached data. */
        private const int _MAX_CHUNK_DATA_SIZE = 64 * 1024;
        /** Length of a compression format name */
        private const int _COMPRESSION_METHOD_NAME_LEN = 32;
        /** Number of allowed different methods */
        private const int _MAX_NUM_COMPRESSION_METHODS = 5; // when we remove patchcompatibilitymode421 we can reduce this to 4

		/** Pak file magic value. */
		public readonly uint Magic;
		/** Pak file version. */
		public readonly EPakFileVersion Version;
		/** Offset to pak file index. */
		public readonly long IndexOffset;
		/** Size (in bytes) of pak file index. */
		public readonly long IndexSize;
		/** SHA1 of the bytes in the index, used to check for data corruption when loading the index. */
		public readonly FSHAHash IndexHash;
		/** Flag indicating if the pak index has been encrypted. */
		public readonly bool bEncryptedIndex;
		/** Encryption key guid. Empty if we should use the embedded key. */
		public readonly FGuid EncryptionKeyGuid;
		/** Compression methods used in this pak file (FNames, saved as FStrings) */
		public readonly string[]? CompressionMethods;

		public FPakInfo(FPakArchive Ar, EPakFileVersion inVersion = EPakFileVersion.PakFile_Version_Latest)
        {
			Magic = 0;
			Version = 0;
			IndexOffset = 0;
			IndexSize = 0;
			IndexHash = default;
			bEncryptedIndex = false;
			EncryptionKeyGuid = default;
			CompressionMethods = null;

			if (Ar.Length < (Ar.Position + GetSerializedSize(inVersion)))
            {
				return;
            }

			if (inVersion >= EPakFileVersion.PakFile_Version_EncryptionKeyGuid)
            {
				EncryptionKeyGuid = Ar.Read<FGuid>();
			}
			bEncryptedIndex = Ar.ReadFlag();
			Magic = Ar.Read<uint>();
			if (Magic != _PAKFILE_MAGIC)
			{
				Magic = 0;
				return;
			}

			Version = Ar.Read<EPakFileVersion>();
			IndexOffset = Ar.Read<long>();
			IndexSize = Ar.Read<long>();
			IndexHash = new FSHAHash(Ar);

			if (Version < EPakFileVersion.PakFile_Version_IndexEncryption)
			{
				bEncryptedIndex = false;
			}

			if (Version < EPakFileVersion.PakFile_Version_EncryptionKeyGuid)
			{
				EncryptionKeyGuid = new FGuid(0u);
			}

			if (Version >= EPakFileVersion.PakFile_Version_FrozenIndex && Version < EPakFileVersion.PakFile_Version_PathHashIndex)
			{
				bool bIndexIsFrozen = Ar.ReadBoolean();
				if (bIndexIsFrozen)
				{
					throw new ParserException(Ar, "PakFile was frozen with version PakFile_Version_FrozenIndex, which is no longer supported by Unreal Engine.");
				}
			}

			if (Version < EPakFileVersion.PakFile_Version_FNameBasedCompressionMethod)
			{
				CompressionMethods = new string[3] { "NAME_Zlib", "NAME_Gzip", "NAME_Oodle" };
			}
			else
			{
				// we need to serialize a known size, so make a buffer of "strings"
				int BufferSize = _COMPRESSION_METHOD_NAME_LEN * _MAX_NUM_COMPRESSION_METHODS;
				byte[] Methods = Ar.ReadBytes(BufferSize);
				CompressionMethods = new string[_MAX_NUM_COMPRESSION_METHODS];
				for (int i = 0; i < CompressionMethods.Length; i++)
				{
					if (Methods[i * _COMPRESSION_METHOD_NAME_LEN] != 0)
					{
						CompressionMethods[i] = Encoding.ASCII.GetString(Methods, i * _COMPRESSION_METHOD_NAME_LEN, _COMPRESSION_METHOD_NAME_LEN).TrimEnd('\0');
					}
				}
			}
		}

		private long GetSerializedSize(EPakFileVersion inVersion)
        {
			long Size = sizeof(uint) + sizeof(int) + sizeof(long) + sizeof(long) + Marshal.SizeOf(typeof(FSHAHash)) + sizeof(byte);
			if (inVersion >= EPakFileVersion.PakFile_Version_EncryptionKeyGuid) Size += Marshal.SizeOf(typeof(FGuid));
			if (inVersion >= EPakFileVersion.PakFile_Version_FNameBasedCompressionMethod) Size += _COMPRESSION_METHOD_NAME_LEN * _MAX_NUM_COMPRESSION_METHODS;
			if (inVersion >= EPakFileVersion.PakFile_Version_FrozenIndex && inVersion < EPakFileVersion.PakFile_Version_PathHashIndex) Size += sizeof(bool);

			return Size;
		}
	}
}
