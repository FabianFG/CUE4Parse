using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Oodle.Objects
{
    public class FOodleDictionaryArchive
    {
        private readonly int MAX_COMPRESS_BUFFER = 1024 * 1024 * 2047;
        private readonly int OODLE_DICTIONARY_SLACK = 65536;
        private FArchive InnerArchive;
        public readonly FDictionaryHeader Header;
        public readonly byte[] DictionaryData;
        public readonly byte[] CompactCompresorState;

        public FOodleDictionaryArchive(FArchive Ar)
        {
            InnerArchive = Ar;
            Header = new FDictionaryHeader(Ar);
            SerializeOodleDecompressData(Header.DictionaryData, out DictionaryData);
            SerializeOodleDecompressData(Header.CompressorData, out CompactCompresorState);
        }

        private bool SerializeOodleDecompressData(FOodleCompressedData DataInfo, out byte [] OutData, bool bOutDataSlack=false)
        {
            OutData = new byte[0];
            if (bOutDataSlack) throw new NotImplementedException(); // Only needed if Archive is loading?

            bool bSuccess = false;
            int DecompressedLength = (int)DataInfo.DecompressedLength;
            int CompressedLength = (int)DataInfo.CompressedLength;
            uint DataOffset = DataInfo.Offset;

            bSuccess = CompressedLength <= InnerArchive.Length - DataOffset;
            bSuccess = bSuccess && DecompressedLength <= MAX_COMPRESS_BUFFER;
            bSuccess = bSuccess && CompressedLength <= MAX_COMPRESS_BUFFER;

            if (bSuccess)
            {
                InnerArchive.Position = DataOffset;

                byte [] DecompressedData = new byte[DecompressedLength];
                if (CompressedLength == DecompressedLength)
                {
                    DecompressedData = InnerArchive.ReadBytes(DecompressedLength);
                    bSuccess = true;
                }
                else
                {
                    byte[] CompressedData = InnerArchive.ReadBytes(CompressedLength + (bOutDataSlack ? OODLE_DICTIONARY_SLACK : 0));
                    try
                    {
                        Compression.Oodle.Decompress(CompressedData, 0, CompressedLength, DecompressedData, 0, DecompressedLength);
                        bSuccess = true;
                    } catch (Compression.OodleException)
                    {
                        bSuccess = false;
                    }
                }

                if (bSuccess)
                {
                    OutData = DecompressedData;
                }
            }
            return bSuccess;
        }
    }
}
