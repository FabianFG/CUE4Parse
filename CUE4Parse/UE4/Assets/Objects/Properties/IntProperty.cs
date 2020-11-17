using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class IntProperty : FPropertyTagType<int>
    {
        public IntProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<int>()
            };
        }
    }
}