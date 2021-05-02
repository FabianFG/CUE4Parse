using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects
{
    public readonly struct FIoOffsetAndLength
    {
        public readonly ulong Offset;
        public readonly ulong Length;

        public FIoOffsetAndLength(FArchive Ar)
        {
            unsafe
            {
                var offsetAndLength = stackalloc byte[10];
                Ar.Serialize(offsetAndLength, 10);
                Offset = offsetAndLength[4]
                         | ((ulong) offsetAndLength[3] << 8)
                         | ((ulong) offsetAndLength[2] << 16)
                         | ((ulong) offsetAndLength[1] << 24)
                         | ((ulong) offsetAndLength[0] << 32);
                Length = offsetAndLength[9]
                         | ((ulong) offsetAndLength[8] << 8)
                         | ((ulong) offsetAndLength[7] << 16)
                         | ((ulong) offsetAndLength[6] << 24)
                         | ((ulong) offsetAndLength[5] << 32);
            }
        }

        public override string ToString()
        {
            return $"{nameof(Offset)} {Offset} | {nameof(Length)} {Length}";
        }
    }
}