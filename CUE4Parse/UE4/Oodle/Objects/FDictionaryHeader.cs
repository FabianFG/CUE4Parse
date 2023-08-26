using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Oodle.Objects
{
    [JsonConverter(typeof(FDictionaryHeaderConverter))]
    public class FDictionaryHeader
    {
        public readonly uint Magic;
        public readonly uint DictionaryVersion;
        public readonly uint OodleMajorHeaderVersion;
        public readonly int HashTableSize;
        public readonly FOodleCompressedData DictionaryData;
        public readonly FOodleCompressedData CompressorData;

        public FDictionaryHeader(FArchive Ar)
        {
            Magic = Ar.Read<uint>();
            DictionaryVersion = Ar.Read<uint>();
            OodleMajorHeaderVersion = Ar.Read<uint>();
            HashTableSize = Ar.Read<int>();
            DictionaryData = Ar.Read<FOodleCompressedData>();
            CompressorData = Ar.Read<FOodleCompressedData>();
        }
    }
}
