using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class Int16Property : FPropertyTagType<short>
    {
        public Int16Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<short>()
            };
        }
    }
}