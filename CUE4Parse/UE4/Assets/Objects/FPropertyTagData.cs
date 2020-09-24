using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class FPropertyTagData
    {
        public class StructProperty : FPropertyTagData
        {
            public FName StructName;
            public FGuid StructGuid;

            public StructProperty(FAssetArchive Ar)
            {
                StructName = Ar.ReadFName();
                StructGuid = Ar.Read<FGuid>();
            }
        }

        public class EnumOrByteProperty : FPropertyTagData
        {
            public FName EnumName;

            public EnumOrByteProperty(FAssetArchive Ar)
            {
                EnumName = Ar.ReadFName();
            }
        }

        public class ArrayOrSetProperty : FPropertyTagData
        {
            public FName InnerType;

            public ArrayOrSetProperty(FAssetArchive Ar)
            {
                InnerType = Ar.ReadFName();
            }
        }

        public class MapProperty : FPropertyTagData
        {
            public FName InnerType;
            public FName ValueType;

            public MapProperty(FAssetArchive Ar)
            {
                InnerType = Ar.ReadFName();
                ValueType = Ar.ReadFName();
            }
        }

        public class BoolProperty : FPropertyTagData
        {
            public byte BoolVal;

            public BoolProperty(FAssetArchive Ar)
            {
                BoolVal = Ar.Read<byte>();
            }
        }
    }
}
