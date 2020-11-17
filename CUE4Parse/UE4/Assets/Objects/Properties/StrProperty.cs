using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class StrProperty : FPropertyTagType<string>
    {
        public StrProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => string.Empty,
                _ => Ar.ReadFString()
            };
        }
    }
}