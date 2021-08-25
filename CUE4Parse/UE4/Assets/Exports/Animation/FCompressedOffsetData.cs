using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class FCompressedOffsetData
    {
        public int[] OffsetData;
        public int StripSize;

        public FCompressedOffsetData(int stripSize = 2)
        {
            StripSize = stripSize;
        }

        public FCompressedOffsetData(FArchive Ar)
        {
            OffsetData = Ar.ReadArray<int>();
            StripSize = Ar.Read<int>();
        }

        public bool IsValid() => StripSize > 0 && OffsetData.Length > 0;
    }
}