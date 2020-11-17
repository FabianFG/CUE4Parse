using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class UInt32Property : FPropertyTagType<uint>
    {
        public UInt32Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<uint>()
            };
        }
    }
}