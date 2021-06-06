using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Oodle.Objects
{
    public class FOodleDictionaryArchive
    {
        private readonly int MAX_COMPRESS_BUFFER = (1024 * 1024 * 2047);
        private readonly int OODLE_DICTIONARY_SLACK = 65536;
        private FArchive InnerArchive;
        public readonly FDictionaryHeader Header;
        public readonly byte[] DictionaryData;
        public readonly byte[] CompactCompresorState;

        public FOodleDictionaryArchive(FArchive Ar)
        {
            InnerArchive = Ar;
            Header = new FDictionaryHeader(Ar);
            SerializeOodleDecompressData(Header.DictionaryData, out DictionaryData, out UInt32 _);
            SerializeOodleDecompressData(Header.CompressorData, out CompactCompresorState, out UInt32 _);
        }

        private bool SerializeOodleDecompressData(FOodleCompressedData DataInfo, out byte [] OutData, out UInt32 OutDataBytes, bool bOutDataSlack=false)
        {
            OutData = new byte[0];
            OutDataBytes = 0;

            bool bSuccess = true;
            int DecompressedLength = (int)DataInfo.DecompressedLength;
            int CompressedLength = (int)DataInfo.CompressedLength;
            UInt32 DataOffset = DataInfo.Offset;

            if (
                CompressedLength <= InnerArchive.Length - DataOffset &&
                DecompressedLength <= MAX_COMPRESS_BUFFER &&
                CompressedLength <= MAX_COMPRESS_BUFFER
            )
            {
                InnerArchive.Position = DataOffset;

                byte [] DecompressedData = new byte[DecompressedLength];
                if (CompressedLength == DecompressedLength)
                {
                    DecompressedData = InnerArchive.ReadBytes(DecompressedLength); // InnerArchive.Serialize(DecompressedData, DecompressedLength);
                }
                else
                {
                    byte[] CompressedData = InnerArchive.ReadBytes(CompressedLength + (bOutDataSlack ? OODLE_DICTIONARY_SLACK : 0));
                    /*int OodleLen = */Compression.Oodle.Decompress(CompressedData, 0, CompressedLength, DecompressedData, 0, DecompressedLength);
                    // bSuccess = OodleLen == DecompressedLength;
                }

                if (bSuccess)
                {
                    OutData = DecompressedData;
                    OutDataBytes = (UInt32)DecompressedLength;
                }
            }
            return bSuccess;
        }
    }
}