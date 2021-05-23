using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Oodle.Objects
{
    public class FOodleCompressedData
    {
        public readonly uint Offset;
        public readonly uint CompressedLength;
        public readonly uint DecompressedLength;

        public FOodleCompressedData(FArchive Ar)
        {
            Offset = Ar.Read<uint>();
            CompressedLength = Ar.Read<uint>();
            DecompressedLength = Ar.Read<uint>();
        }
    }
}