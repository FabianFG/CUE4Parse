using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class InterfaceProperty : FPropertyTagType<UInterfaceProperty>
    {
        public InterfaceProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new UInterfaceProperty(),
                _ => Ar.Read<UInterfaceProperty>()
            };
        }
    }
}