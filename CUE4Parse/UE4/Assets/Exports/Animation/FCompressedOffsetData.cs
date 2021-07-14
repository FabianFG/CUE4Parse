using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class FCompressedOffsetData
    {
        public int[] OffsetData;
        public int StripSize;

        public FCompressedOffsetData(FArchive Ar)
        {
            OffsetData = Ar.ReadArray<int>();
            StripSize = Ar.Read<int>();
        }
    }
}