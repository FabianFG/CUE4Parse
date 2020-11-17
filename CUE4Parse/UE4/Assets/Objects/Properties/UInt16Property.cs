using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class UInt16Property : FPropertyTagType<ushort>
    {
        public UInt16Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<ushort>()
            };
        }
    }
}