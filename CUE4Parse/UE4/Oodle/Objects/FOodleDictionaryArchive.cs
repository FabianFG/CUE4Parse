using CUE4Parse.Compression;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Oodle.Objects
{
    public class FOodleDictionaryArchive
    {
        private readonly int MAX_COMPRESS_BUFFER = 1024 * 1024 * 2047;
        private readonly FArchive _innerArchive;

        public readonly FDictionaryHeader Header;
        public readonly byte[] DictionaryData;
        public readonly byte[] CompactCompresorState;

        public FOodleDictionaryArchive(FArchive Ar)
        {
            _innerArchive = Ar;
            Header = new FDictionaryHeader(Ar);
            SerializeOodleDecompressData(Header.DictionaryData, out DictionaryData);
            SerializeOodleDecompressData(Header.CompressorData, out CompactCompresorState);
        }

        private bool SerializeOodleDecompressData(FOodleCompressedData dataInfo, out byte[] outData)
        {
            outData = [];
            var decompressedLength = (int) dataInfo.DecompressedLength;
            var compressedLength = (int) dataInfo.CompressedLength;
            if (compressedLength > _innerArchive.Length - dataInfo.Offset || decompressedLength > MAX_COMPRESS_BUFFER ||
                compressedLength > MAX_COMPRESS_BUFFER) return false;

            _innerArchive.Position = dataInfo.Offset;
            if (compressedLength == decompressedLength)
            {
                outData = _innerArchive.ReadBytes(decompressedLength);
            }
            else
            {
                outData = new byte[decompressedLength];
                var compressedData = _innerArchive.ReadBytes(compressedLength);
                OodleHelper.Decompress(compressedData, 0, compressedLength, outData, 0, decompressedLength);
            }

            return outData.Length == decompressedLength;
        }
    }
}
